using OAuth.Contracts.Dto;

namespace HttpConnection.OAuth;

internal static class OAuthTokenResponseValidator
{
    internal static TokenResponseDto Validate(
        TokenResponseDto response,
        OAuthResponseKind responseKind
        )
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.HasError)
        {
            throw OAuthExceptionFactory.CreateProtocolException(
                response.Error,
                response.ErrorDescription
                );
        }

        if (response.HasErrorDescription)
        {
            throw OAuthExceptionFactory.CreateResponseValidationException(
                responseKind,
                "ErrorDescription is present without an OAuth error code."
                );
        }

        if (!response.HasAccessToken)
        {
            throw OAuthExceptionFactory.CreateResponseValidationException(
                responseKind,
                "AccessToken is missing."
                );
        }

        if (response.ExpiresIn <= 0)
        {
            throw OAuthExceptionFactory.CreateResponseValidationException(
                responseKind,
                "ExpiresIn must be greater than zero."
                );
        }

        return response;
    }
}
