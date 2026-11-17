using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OAuth.Contracts.Dto;
using System.Net;
using System.Net.Http;

namespace HttpConnection.OAuth;

internal sealed class OAuthApiProvider
{
    private readonly HttpRestApiClient _httpClient;
    private readonly OAuthOptions _options;

    private readonly IDocumentRetriever _documentRetriever;
    private readonly SemaphoreSlim _configurationLock = new(1, 1);

    private OpenIdConnectConfiguration? _configuration;

    public OAuthApiProvider(
        HttpRestApiClient httpClient,
        IOptions<OAuthOptions> options
        )
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _documentRetriever = new OAuthHttpDocumentRetriever(httpClient);
        _options = options.Value;
    }

    internal async Task<OAuthMapDto> GetOAuthMapAsync(
        CancellationToken cancellationToken
        )
    {
        OpenIdConnectConfiguration configuration =
            await GetOidcConfigurationAsync(cancellationToken)
                .ConfigureAwait(false);

        return OAuthServerMapFactory.Create(configuration);
    }

    internal async Task<OpenIdConnectConfiguration> GetOidcConfigurationAsync(
        CancellationToken cancellationToken
        )
    {
        OpenIdConnectConfiguration? configuration =
            Volatile.Read(ref _configuration);

        if (configuration != null)
            return configuration;

        await _configurationLock
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            configuration = _configuration;

            if (configuration != null)
                return configuration;

            configuration = await LoadOidcConfigurationAsync(cancellationToken)
                .ConfigureAwait(false);

            Volatile.Write(ref _configuration, configuration);

            return configuration;
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    internal async Task<OpenIdConnectConfiguration>
    LoadOidcConfigurationAsync(CancellationToken cancellationToken)
    {
        string metadataAddress =
            $"{_options.Authority.TrimEnd('/')}/.well-known/openid-configuration";

        OpenIdConnectConfiguration configuration =
            await OpenIdConnectConfigurationRetriever.GetAsync(
                metadataAddress,
                _documentRetriever,
                cancellationToken
                )
            .ConfigureAwait(false);

        OAuthMapDto serverMap = OAuthServerMapFactory.Create(configuration);

        OAuthServerMapValidator.Validate(
            serverMap,
            _options.Authority
            );

        return configuration;
    }

    internal async Task<TokenResponseDto> ExchangeCodeForTokensAsync(
        string code,
        string verifier,
        CancellationToken cancellationToken
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(verifier);

        var redirectUri = new Uri(_options.RedirectUri);

        OAuthMapDto serverMap = await GetOAuthMapAsync(cancellationToken)
            .ConfigureAwait(false);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = redirectUri.AbsoluteUri,
            ["code"] = code,
            ["code_verifier"] = verifier
        });

        try
        {
            TokenResponseDto response = await _httpClient
                .PostContentAsync<TokenResponseDto>(
                    serverMap.TokenEndpoint,
                    content,
                    cancellationToken
                    )
                .ConfigureAwait(false);

            return OAuthTokenResponseValidator.Validate(
                response,
                OAuthResponseKind.AuthorizationCodeToken
                );
        }
        catch (HttpRestResponseException<TokenResponseDto> exception)
        when (IsOAuthErrorStatus(exception.StatusCode))
        {
            return OAuthTokenResponseValidator.Validate(
                exception.Response,
                OAuthResponseKind.AuthorizationCodeToken
                );
        }
    }

    public async Task<TokenResponseDto> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        OAuthMapDto serverMap = await GetOAuthMapAsync(cancellationToken)
            .ConfigureAwait(false);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["refresh_token"] = refreshToken
        });

        try
        {
            TokenResponseDto response = await _httpClient
                .PostContentAsync<TokenResponseDto>(
                    serverMap.TokenEndpoint,
                    content,
                    cancellationToken
                    )
                .ConfigureAwait(false);

            return OAuthTokenResponseValidator.Validate(
                response,
                OAuthResponseKind.RefreshToken
                );
        }
        catch (HttpRestResponseException<TokenResponseDto> exception)
        when (IsOAuthErrorStatus(exception.StatusCode))
        {
            return OAuthTokenResponseValidator.Validate(
                exception.Response,
                OAuthResponseKind.RefreshToken
                );
        }
    }

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        OAuthMapDto serverMap = await GetOAuthMapAsync(cancellationToken)
            .ConfigureAwait(false);

        string revocationEndpoint =
            OAuthServerMapValidator.GetRequiredRevocationEndpoint(serverMap);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token"
        });

        await _httpClient
            .PostContentAsync(
                revocationEndpoint,
                content,
                cancellationToken
                )
            .ConfigureAwait(false);
    }

    private static bool IsOAuthErrorStatus(HttpStatusCode? statusCode)
    {
        return statusCode is
            HttpStatusCode.BadRequest or
            HttpStatusCode.Unauthorized;
    }
}
