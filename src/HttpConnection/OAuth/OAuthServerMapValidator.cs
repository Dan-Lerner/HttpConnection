using OAuth.Contracts.Dto;

namespace HttpConnection.OAuth;

internal static class OAuthServerMapValidator
{
    internal static string GetRequiredRevocationEndpoint(OAuthMapDto serverMap)
    {
        ArgumentNullException.ThrowIfNull(serverMap);

        if (string.IsNullOrWhiteSpace(serverMap.RevocationEndpoint))
        {
            throw OAuthExceptionFactory.CreateEndpointUnavailableException(
                "revocation"
                );
        }

        return serverMap.RevocationEndpoint;
    }

    internal static OAuthMapDto Validate(
        OAuthMapDto serverMap,
        string expectedIssuer
        )
    {
        ArgumentNullException.ThrowIfNull(serverMap);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedIssuer);

        ValidateIssuer(
            serverMap.Issuer,
            expectedIssuer
            );
        ValidateRequiredHttpUri(
            serverMap.AuthorizationEndpoint,
            nameof(serverMap.AuthorizationEndpoint)
            );
        ValidateRequiredHttpUri(
            serverMap.TokenEndpoint,
            nameof(serverMap.TokenEndpoint)
            );
        ValidateOptionalHttpUri(
            serverMap.RevocationEndpoint,
            nameof(serverMap.RevocationEndpoint)
            );

        return serverMap;
    }

    private static Uri ValidateRequiredHttpUri(
        string? value,
        string valueName
        )
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            !IsHttpScheme(uri))
        {
            throw OAuthExceptionFactory.CreateResponseValidationException(
                OAuthResponseKind.ServerMetadata,
                $"{valueName} must be an absolute HTTP or HTTPS URI."
                );
        }

        return uri;
    }

    private static void ValidateOptionalHttpUri(
        string? value,
        string valueName
        )
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        ValidateRequiredHttpUri(
            value,
            valueName
            );
    }

    private static void ValidateIssuer(
        string issuer,
        string expectedIssuer
        )
    {
        Uri issuerUri = ValidateRequiredHttpUri(
            issuer,
            nameof(OAuthMapDto.Issuer)
            );
        Uri expectedIssuerUri = ValidateRequiredHttpUri(
            expectedIssuer,
            nameof(expectedIssuer)
            );

        string normalizedIssuer =
            issuerUri.AbsoluteUri.TrimEnd('/');
        string normalizedExpectedIssuer =
            expectedIssuerUri.AbsoluteUri.TrimEnd('/');

        if (!StringComparer.Ordinal.Equals(
            normalizedIssuer,
            normalizedExpectedIssuer
            ))
        {
            throw OAuthExceptionFactory.CreateResponseValidationException(
                OAuthResponseKind.ServerMetadata,
                "Issuer does not match the configured authority."
                );
        }
    }

    private static bool IsHttpScheme(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp ||
            uri.Scheme == Uri.UriSchemeHttps;
    }
}
