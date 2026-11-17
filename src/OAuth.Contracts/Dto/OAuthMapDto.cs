namespace OAuth.Contracts.Dto;

public sealed record OAuthMapDto(
    string Issuer,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string? RevocationEndpoint
    );

