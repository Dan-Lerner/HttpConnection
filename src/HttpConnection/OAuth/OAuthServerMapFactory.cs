using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OAuth.Contracts.Dto;

namespace HttpConnection.OAuth;

internal static class OAuthServerMapFactory
{
    internal static OAuthMapDto Create(
        OpenIdConnectConfiguration configuration
        )
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new OAuthMapDto(
            configuration.Issuer,
            configuration.AuthorizationEndpoint,
            configuration.TokenEndpoint,
            configuration.RevocationEndpoint
            );
    }
}
