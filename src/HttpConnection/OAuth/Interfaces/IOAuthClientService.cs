namespace HttpConnection.OAuth.Interfaces;

public interface IOAuthClientService
{
    bool HasValidAccessToken();

    bool HasRefreshToken();

    void RevokeAccessToken(string accessToken);

    Task<string?> GetAccessTokenAsync(CancellationToken ct);

    Task SetUserAsync(
        string identity,
        CancellationToken ct
        );

    Task<OAuthLoginResult> LoginInteractiveAsync(
        string? userName,
        bool forceReauthentication,
        CancellationToken ct
        );

    Task RevokeAccessAsync(CancellationToken ct);

    Task LogoutAsync(CancellationToken ct);
}
