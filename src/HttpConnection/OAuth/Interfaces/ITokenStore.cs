namespace HttpConnection.OAuth.Interfaces;

internal interface ITokenStore
{
    TokenSession? Session { get; }

    Task SetUserAsync(string user, CancellationToken ct);

    bool IsAccessTokenValid();

    bool HasRefreshToken();

    string? GetAccessToken();

    string? GetRefreshToken();

    void RevokeAccessToken(string? accessToken);

    Task SetSessionAsync(
        TokenSession session,
        CancellationToken ct
        );

    Task SetRefreshTokenAsync(
        string token,
        CancellationToken ct
        );

    Task<bool> LoadAsync(
        CancellationToken ct
        );

    Task ClearAsync(
        CancellationToken ct
        );
}
