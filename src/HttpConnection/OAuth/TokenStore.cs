using HttpConnection.OAuth.Interfaces;
using ExampleWPFClient.Infrastructure.SecureStore;
using Microsoft.Extensions.Options;

namespace HttpConnection.OAuth;

internal sealed class TokenStore :
    ITokenStore
{
    private readonly ISecureStorageFactory? _secureStorageFactory;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _accessTokenSafetyWindow;

    private ISecureStorage<OAuthPersistedSession>? _persistentStorage;

    public TokenStore(
        ISecureStorageFactory? secureStorageFactory,
        IOptions<OAuthOptions> options,
        TimeProvider timeProvider
        )
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _secureStorageFactory = secureStorageFactory;
        _timeProvider = timeProvider;
        _accessTokenSafetyWindow = TimeSpan.FromSeconds(
            options.Value.AccessTokenSafetyWindowSeconds
            );
    }

    private TokenSession? _session;
    public TokenSession? Session
        => Volatile.Read(ref _session);

    public async Task SetUserAsync(
        string identity,
        CancellationToken ct
        )
    {
        ArgumentNullException.ThrowIfNull(identity);

        Interlocked.Exchange(ref _session, null);

        _persistentStorage = _secureStorageFactory?
            .Create<OAuthPersistedSession>(identity);

        await LoadAsync(ct);
    }

    public bool IsAccessTokenValid()
    {
        TokenSession? session = Session;

        return session?.IsAccessTokenUsable(
            _timeProvider.GetUtcNow(),
            _accessTokenSafetyWindow
            ) == true;
    }

    public bool HasRefreshToken()
        => Session?.HasRefreshToken == true;

    public string? GetAccessToken()
        => Session?.AccessToken;

    public string? GetRefreshToken()
        => Session?.RefreshToken;

    public void RevokeAccessToken(string? accessToken)
    {
        while (true)
        {
            TokenSession? session = Session;

            if (session == null ||
                !session.IsAccessToken(accessToken))
            {
                return;
            }

            TokenSession updatedSession = session.WithoutAccessToken();

            TokenSession? currentSession = Interlocked.CompareExchange(
                ref _session,
                updatedSession,
                session
                );

            if (ReferenceEquals(currentSession, session))
                return;
        }
    }

    public async Task SetSessionAsync(
        TokenSession session,
        CancellationToken ct
        )
    {
        ArgumentNullException.ThrowIfNull(session);

        if (_persistentStorage != null)
        {
            await _persistentStorage.SaveAsync(
                new OAuthPersistedSession(session),
                ct
                );
        }

        Interlocked.Exchange(ref _session, session);
    }

    public Task SetRefreshTokenAsync(
        string token,
        CancellationToken ct
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        TokenSession? session = Session;

        var updatedSession = new TokenSession(
            session?.AccessToken,
            token,
            session?.AccessTokenExpiresAt ?? DateTimeOffset.MinValue
            );

        return SetSessionAsync(updatedSession, ct);
    }

    public async Task<bool> LoadAsync(CancellationToken ct)
    {
        if (_persistentStorage == null)
            return false;

        OAuthPersistedSession? session = await _persistentStorage.LoadAsync(ct);

        if (session == null || string.IsNullOrWhiteSpace(session.RefreshToken))
            return false;

        var restoredSession = new TokenSession(
            null,
            session.RefreshToken,
            DateTimeOffset.MinValue
            );

        Interlocked.Exchange(ref _session, restoredSession);

        return true;
    }

    public async Task ClearAsync(CancellationToken ct)
    {
        Interlocked.Exchange(ref _session, null);

        if (_persistentStorage != null)
            await _persistentStorage.ClearAsync(ct);
    }
}
