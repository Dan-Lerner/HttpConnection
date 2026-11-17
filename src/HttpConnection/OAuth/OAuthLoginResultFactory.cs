using System.Security.Claims;

namespace HttpConnection.OAuth;

internal static class OAuthLoginResultFactory
{
    internal static OAuthLoginResult Create(
        ClaimsPrincipal principal,
        string? fallbackUserName
        )
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? identity = principal.FindFirstValue(
            "sub",
            ClaimTypes.NameIdentifier
            );

        if (string.IsNullOrWhiteSpace(identity))
        {
            throw OAuthExceptionFactory.CreateResponseValidationException(
                OAuthResponseKind.IdToken,
                "The subject identifier is missing."
                );
        }

        string? userName = principal.FindFirstValue(
            "preferred_username",
            ClaimTypes.Name
            )
            ?? principal.Identity?.Name
            ?? fallbackUserName;

        return new OAuthLoginResult
        {
            Identity = identity,
            UserName = string.IsNullOrWhiteSpace(userName) ? null : userName
        };
    }
}
