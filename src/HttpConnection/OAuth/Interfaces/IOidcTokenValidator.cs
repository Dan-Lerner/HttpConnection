using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OAuth.Contracts.Dto;
using System.Security.Claims;

namespace HttpConnection.OAuth.Interfaces;

public interface IOidcTokenValidator
{
    Task<ClaimsPrincipal?> ValidateAsync(
        TokenResponseDto response,
        OpenIdConnectConfiguration configuration,
        string expectedNonce,
        CancellationToken cancellationToken
        );
}
