namespace HttpConnection.OAuth;

internal enum OAuthResponseKind
{
    ServerMetadata,
    AuthorizationCodeToken,
    RefreshToken,
    IdToken
}

internal enum OAuthSessionUnavailableReason
{
    RefreshTokenMissing,
    AccessTokenMissing
}

internal abstract class OAuthException :
    Exception
{
    protected OAuthException(string message)
        : base(message)
    {
    }
}

internal sealed class OAuthProtocolException :
    OAuthException
{
    internal OAuthProtocolException(
        string error,
        string errorDescription
        )
        : base($"{error}: {errorDescription}")
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    internal string Error { get; }

    internal string ErrorDescription { get; }

    internal bool IsInvalidGrant
    {
        get
        {
            return StringComparer.Ordinal.Equals(
                Error,
                "invalid_grant"
                );
        }
    }
}

internal sealed class OAuthResponseValidationException :
    OAuthException
{
    internal OAuthResponseValidationException(
        OAuthResponseKind responseKind,
        string message
        )
        : base(message)
    {
        ResponseKind = responseKind;
    }

    internal OAuthResponseKind ResponseKind { get; }
}

internal sealed class OAuthEndpointUnavailableException :
    OAuthException
{
    internal OAuthEndpointUnavailableException(
        string endpointName,
        string message
        )
        : base(message)
    {
        EndpointName = endpointName;
    }

    internal string EndpointName { get; }
}

internal sealed class OAuthSessionUnavailableException :
    OAuthException
{
    internal OAuthSessionUnavailableException(
        OAuthSessionUnavailableReason reason,
        string message
        )
        : base(message)
    {
        Reason = reason;
    }

    internal OAuthSessionUnavailableReason Reason { get; }
}

internal static class OAuthExceptionFactory
{
    private const string DEFAULT_PROTOCOL_ERROR_DESCRIPTION =
        "The authorization server returned an OAuth protocol error.";

    internal static OAuthProtocolException CreateProtocolException(
        string error,
        string? errorDescription
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        string description = string.IsNullOrWhiteSpace(errorDescription)
            ? DEFAULT_PROTOCOL_ERROR_DESCRIPTION
            : errorDescription;

        return new OAuthProtocolException(
            error,
            description
            );
    }

    internal static OAuthResponseValidationException CreateResponseValidationException(
        OAuthResponseKind responseKind,
        string validationMessage
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationMessage);

        return new OAuthResponseValidationException(
            responseKind,
            $"{GetResponseName(responseKind)} is invalid: {validationMessage}"
            );
    }

    internal static OAuthSessionUnavailableException CreateSessionUnavailableException(
        OAuthSessionUnavailableReason reason
        )
    {
        string message = reason switch
        {
            OAuthSessionUnavailableReason.RefreshTokenMissing =>
                "The OAuth session does not contain a refresh token.",
            OAuthSessionUnavailableReason.AccessTokenMissing =>
                "The OAuth session does not contain a valid access token.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "Unknown OAuth session-unavailable reason."
                )
        };

        return new OAuthSessionUnavailableException(
            reason,
            message
            );
    }

    internal static OAuthEndpointUnavailableException CreateEndpointUnavailableException(
        string endpointName
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

        return new OAuthEndpointUnavailableException(
            endpointName,
            $"The authorization server does not advertise the {endpointName} endpoint."
            );
    }

    private static string GetResponseName(OAuthResponseKind responseKind)
    {
        return responseKind switch
        {
            OAuthResponseKind.ServerMetadata => "OAuth server metadata",
            OAuthResponseKind.AuthorizationCodeToken =>
                "OAuth authorization-code token response",
            OAuthResponseKind.RefreshToken => "OAuth refresh-token response",
            OAuthResponseKind.IdToken => "OIDC ID token",
            _ => throw new ArgumentOutOfRangeException(
                nameof(responseKind),
                responseKind,
                "Unknown OAuth response kind."
                )
        };
    }
}
