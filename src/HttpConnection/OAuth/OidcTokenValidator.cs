using HttpConnection.Helpers;
using HttpConnection.OAuth.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OAuth.Contracts.Dto;
using System.Security.Claims;

namespace HttpConnection.OAuth;

internal sealed class OidcTokenValidator :
    IOidcTokenValidator
{
    private readonly OAuthOptions _options;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public OidcTokenValidator(
        IOptions<OAuthOptions> options
        )
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(
        TokenResponseDto response,
        OpenIdConnectConfiguration configuration,
        string expectedNonce,
        CancellationToken ct
        )
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedNonce);

        if (!response.HasIdToken)
            return null;

        var validationParameters = new TokenValidationParameters
        {
            RequireSignedTokens = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,

            ValidateIssuer = true,
            ValidIssuer = configuration.Issuer,

            ValidateAudience = true,
            ValidAudience = _options.ClientId,

            ValidateLifetime = true,

            ClockSkew = TimeSpan.FromMinutes(1)
        };

        TokenValidationResult result = await _tokenHandler
            .ValidateTokenAsync(
                response.IdToken,
                validationParameters
                );

        if (!result.IsValid || result.ClaimsIdentity == null)
            return null;

        ClaimsIdentity identity = result.ClaimsIdentity;

        string? actualNonce = identity.FindFirst("nonce")?.Value;

        if (!AuthCryptographicOperations
            .CryptographicEquals(actualNonce, expectedNonce))
        {
            return null;
        }

        return new ClaimsPrincipal(identity);
    }
}
