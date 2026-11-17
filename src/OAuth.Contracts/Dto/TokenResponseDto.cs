using System.Diagnostics.CodeAnalysis;

namespace OAuth.Contracts.Dto;

public sealed class TokenResponseDto
{
    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public string? IdToken { get; set; }

    public int ExpiresIn { get; set; }

    public string? Error { get; set; }

    public string? ErrorDescription { get; set; }

    [MemberNotNullWhen(true, nameof(AccessToken))]
    public bool HasAccessToken
        => !string.IsNullOrWhiteSpace(AccessToken);

    [MemberNotNullWhen(true, nameof(Error))]
    public bool HasError
        => !string.IsNullOrWhiteSpace(Error);

    [MemberNotNullWhen(true, nameof(ErrorDescription))]
    public bool HasErrorDescription
        => !string.IsNullOrWhiteSpace(ErrorDescription);

    [MemberNotNullWhen(true, nameof(IdToken))]
    public bool HasIdToken
        => !string.IsNullOrWhiteSpace(IdToken);
}
