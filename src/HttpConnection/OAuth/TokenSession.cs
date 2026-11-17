using OAuth.Contracts.Dto;

namespace HttpConnection.OAuth;

public sealed record TokenSession(
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset AccessTokenExpiresAt
    )
{
    public bool HasAccessToken
        => !string.IsNullOrWhiteSpace(AccessToken);

    public bool HasRefreshToken
        => !string.IsNullOrWhiteSpace(RefreshToken);

    public bool IsAccessToken(string? accessToken)
    {
        return HasAccessToken &&
            StringComparer.Ordinal.Equals(
                AccessToken,
                accessToken
                );
    }

    public bool IsAccessTokenUsable(
        DateTimeOffset currentTime,
        TimeSpan safetyWindow
        )
    {
        if (safetyWindow < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(safetyWindow));

        return HasAccessToken &&
            AccessTokenExpiresAt > currentTime.Add(safetyWindow);
    }

    public TokenSession WithoutAccessToken()
    {
        return this with
        {
            AccessToken = null,
            AccessTokenExpiresAt = DateTimeOffset.MinValue
        };
    }
}

public static class TokenSessionBuilder
{
    public static TokenSession Create(
        TokenResponseDto dto,
        string? previousRefreshToken,
        DateTimeOffset currentTime
        )
    {
        return new TokenSession(
            dto.AccessToken,
            dto.RefreshToken ?? previousRefreshToken,
            currentTime.AddSeconds(dto.ExpiresIn)
            );
    }
}
