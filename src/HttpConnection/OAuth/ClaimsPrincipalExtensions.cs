using System.Security.Claims;

namespace HttpConnection.OAuth;

internal static class ClaimsPrincipalExtensions
{
    internal static string? FindFirstValue(
        this ClaimsPrincipal principal,
        params string[] claimTypes
        )
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(claimTypes);

        foreach (string claimType in claimTypes)
        {
            string? value = principal.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
