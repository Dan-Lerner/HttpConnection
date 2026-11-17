namespace HttpConnection.OAuth;

public sealed record OAuthLoginResult
{
    public required string Identity { get; init; }

    public string? UserName { get; init; }
}
