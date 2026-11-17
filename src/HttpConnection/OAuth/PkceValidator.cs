using HttpConnection.Helpers;

namespace HttpConnection.OAuth;

internal class PkceValidator
{
    public static void ValidateAuthorizationCallback(
        OAuthCallbackResult callback,
        PkceSession session
        )
    {

        if (!AuthCryptographicOperations
            .CryptographicEquals(callback.State, session.State))
        {
            throw OAuthExceptionFactory.CreateProtocolException(
                "invalid_state",
                "The state parameter is wrong."
                );
        }

        if (callback.Error != null)
        {
            throw OAuthExceptionFactory.CreateProtocolException(
                callback.Error,
                callback.ErrorDescription ?? "Authorization failed."
                );
        }

        if (string.IsNullOrWhiteSpace(callback.Code))
        {
            throw OAuthExceptionFactory.CreateProtocolException(
                "invalid_response",
                "There is no an authorization code in the callback."
                );
        }
    }
}
