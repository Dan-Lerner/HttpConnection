using HttpConnection.Helpers;
using HttpConnection.OAuth.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OAuth.Contracts.Dto;
using System.Security.Claims;

namespace HttpConnection.OAuth;

internal sealed class OAuthClientService :
    IOAuthClientService
{
    #region Fields

    private readonly OAuthApiProvider _oauthProvider;
    private readonly ITokenStore _tokenStore;
    private readonly IOidcTokenValidator _tokenValidator;
    private readonly OAuthOptions _options;
    private readonly IOuterLockStatus? _outerLockStatus;
    private readonly TimeProvider _timeProvider;

    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    #endregion

    #region Constructors

    public OAuthClientService(
        OAuthApiProvider oauthProvider,
        ITokenStore tokenStore,
        IOidcTokenValidator tokenValidator,
        IOptions<OAuthOptions> options,
        IOuterLockStatus? outerLockStatus,
        TimeProvider timeProvider
        )
    {
        ArgumentNullException.ThrowIfNull(oauthProvider);
        ArgumentNullException.ThrowIfNull(tokenStore);
        ArgumentNullException.ThrowIfNull(tokenValidator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _oauthProvider = oauthProvider;
        _tokenStore = tokenStore;
        _tokenValidator = tokenValidator;
        _options = options.Value;
        _outerLockStatus = outerLockStatus;
        _timeProvider = timeProvider;
    }

    #endregion

    #region Properties

    private bool IsLockedOutside
        => _outerLockStatus?.Locked ?? false;

    #endregion

    #region Public methods

    public bool HasValidAccessToken()
        => _tokenStore.IsAccessTokenValid();

    public bool HasRefreshToken()
        => _tokenStore.HasRefreshToken();

    public void RevokeAccessToken(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        _tokenStore.RevokeAccessToken(accessToken);
    }

    public async Task SetUserAsync(
        string identity,
        CancellationToken ct
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);

        await _tokenLock
            .WaitAsync(ct)
            .ConfigureAwait(false);

        try
        {
            await _tokenStore
                .SetUserAsync(
                    identity,
                    ct
                    )
                .ConfigureAwait(false);

            bool isRefreshTokenValid =
                await ValidateRefreshTokenAsync(ct)
                    .ConfigureAwait(false);

            if (!isRefreshTokenValid)
            {
                await _tokenStore
                    .ClearAsync(ct)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        if (IsLockedOutside)
            return null;

        await _tokenLock
            .WaitAsync(ct)
            .ConfigureAwait(false);

        try
        {
            if (IsLockedOutside)
                return null;

            if (_tokenStore.IsAccessTokenValid())
            {
                string? currentAccessToken = _tokenStore.GetAccessToken();

                if (!string.IsNullOrWhiteSpace(currentAccessToken))
                    return IsLockedOutside ? null : currentAccessToken;
            }

            string? refreshToken = _tokenStore.GetRefreshToken();

            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            TokenSession session;

            try
            {
                session = await RefreshSessionAsync(
                    refreshToken,
                    ct
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            when (IsRefreshRejected(exception))
            {
                return null;
            }

            TokenSession sessionToStore = IsLockedOutside
                ? session.WithoutAccessToken()
                : session;

            await _tokenStore
                .SetSessionAsync(
                    sessionToStore,
                    ct
                    )
                .ConfigureAwait(false);

            if (IsLockedOutside)
            {
                _tokenStore.RevokeAccessToken(session.AccessToken);

                return null;
            }

            return session.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<OAuthLoginResult> LoginInteractiveAsync(
        string? user,
        bool forceReauthentication,
        CancellationToken ct
        )
    {
        OAuthMapDto serverMap = await _oauthProvider
            .GetOAuthMapAsync(ct)
            .ConfigureAwait(false);

        PkceSession authorization = PkceSession.Create();

        using var callbackListener = new OAuthCallbackListener(
            _options.RedirectUri
            );

        callbackListener.Start();

        string authorizationUri = CreateAuthorizationUri(
            serverMap,
            authorization,
            user,
            forceReauthentication
            );

        SystemOperations.OpenInBrowser(authorizationUri);

        OAuthCallbackResult callback = await callbackListener
            .WaitAsync(
                TimeSpan.FromSeconds(_options.CallbackTimeoutSeconds),
                ct
                )
            .ConfigureAwait(false);

        PkceValidator.ValidateAuthorizationCallback(
            callback,
            authorization
            );

        TokenResponseDto tokens = await _oauthProvider
            .ExchangeCodeForTokensAsync(
                callback.Code!,
                authorization.CodeVerifier,
                ct
                )
            .ConfigureAwait(false);

        OpenIdConnectConfiguration configuration = await _oauthProvider
            .GetOidcConfigurationAsync(ct)
            .ConfigureAwait(false);

        ClaimsPrincipal? validatedPrincipal = await _tokenValidator
            .ValidateAsync(
                tokens,
                configuration,
                authorization.Nonce,
                ct
                )
            .ConfigureAwait(false);

        ClaimsPrincipal principal = validatedPrincipal
            ?? throw OAuthExceptionFactory.CreateResponseValidationException(
                OAuthResponseKind.IdToken,
                "ID token validation failed."
                );

        OAuthLoginResult loginResult = OAuthLoginResultFactory.Create(
            principal,
            user
            );

        TokenSession session = TokenSessionBuilder.Create(
            tokens,
            null,
            _timeProvider.GetUtcNow()
            );

        await _tokenLock
            .WaitAsync(ct)
            .ConfigureAwait(false);

        try
        {
            await _tokenStore
                .SetUserAsync(
                    loginResult.Identity,
                    ct
                    )
                .ConfigureAwait(false);

            await _tokenStore
                .SetSessionAsync(
                    session,
                    ct
                    )
                .ConfigureAwait(false);
        }
        finally
        {
            _tokenLock.Release();
        }

        return loginResult;
    }

    public async Task RevokeAccessAsync(CancellationToken ct)
    {
        await _tokenLock
            .WaitAsync(ct)
            .ConfigureAwait(false);

        try
        {
            string? accessToken = _tokenStore.GetAccessToken();

            _tokenStore.RevokeAccessToken(accessToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task LogoutAsync(CancellationToken ct)
    {
        string? refreshToken;

        await _tokenLock
            .WaitAsync(ct)
            .ConfigureAwait(false);

        try
        {
            refreshToken = _tokenStore.GetRefreshToken();

            await _tokenStore
                .ClearAsync(ct)
                .ConfigureAwait(false);
        }
        finally
        {
            _tokenLock.Release();
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        await _oauthProvider
            .RevokeRefreshTokenAsync(
                refreshToken,
                ct
                )
            .ConfigureAwait(false);
    }

    #endregion

    #region Private methods


    private string CreateAuthorizationUri(
        OAuthMapDto serverMap,
        PkceSession session,
        string? user,
        bool forceReauthentication
        )
    {
        var redirectUri = new Uri(_options.RedirectUri);

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = redirectUri.AbsoluteUri,
            ["response_type"] = "code",
            ["scope"] = _options.Scopes,
            ["code_challenge"] = session.CodeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = session.State,
            ["nonce"] = session.Nonce
        };

        if (!string.IsNullOrWhiteSpace(user))
            parameters["login_hint"] = user;

        if (forceReauthentication)
            parameters["prompt"] = "login";

        return UriOperations.BuildUri(
            serverMap.AuthorizationEndpoint,
            parameters);
    }

    private static bool IsRefreshRejected(Exception exception)
    {
        return exception is OAuthProtocolException
        {
            IsInvalidGrant: true
        };
    }

    private async Task<TokenSession> RefreshSessionAsync(
        string previousRefreshToken,
        CancellationToken ct
        )
    {
        try
        {
            TokenResponseDto response = await _oauthProvider
                .RefreshAsync(
                    previousRefreshToken,
                    ct
                    )
                .ConfigureAwait(false);

            return TokenSessionBuilder.Create(
                response,
                previousRefreshToken,
                _timeProvider.GetUtcNow()
                );
        }
        catch (Exception exception)
        when (IsRefreshRejected(exception))
        {
            await _tokenStore
                .ClearAsync(ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    private async Task<bool> ValidateRefreshTokenAsync(
        CancellationToken ct
        )
    {
        string? previousRefreshToken = _tokenStore.GetRefreshToken();

        if (string.IsNullOrWhiteSpace(previousRefreshToken))
            return false;

        TokenSession session = await RefreshSessionAsync(
            previousRefreshToken,
            ct
            )
            .ConfigureAwait(false);

        if (!session.HasRefreshToken)
        {
            throw OAuthExceptionFactory.CreateSessionUnavailableException(
                OAuthSessionUnavailableReason.RefreshTokenMissing
                );
        }

        await _tokenStore
            .SetRefreshTokenAsync(
                session.RefreshToken!,
                ct
                )
            .ConfigureAwait(false);

        return true;
    }

    #endregion
}
