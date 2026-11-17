namespace HttpConnection.OAuth;

internal class OAuthPersistedSession
{
    public OAuthPersistedSession()
    { }

    public OAuthPersistedSession(
        TokenSession session
        )
    {
        RefreshToken = session.RefreshToken;
    }

    public string? RefreshToken { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }
}
