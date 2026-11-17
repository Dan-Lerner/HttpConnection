namespace HttpConnection.OAuth;

public sealed record OAuthCallbackResult(
    string? Code,
    string? State,
    string? Error,
    string? ErrorDescription
    );
