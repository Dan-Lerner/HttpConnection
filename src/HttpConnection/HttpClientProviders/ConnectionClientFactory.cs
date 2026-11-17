using HttpConnection.OAuth.Interfaces;
using HttpConnection.Settings;

namespace HttpConnection.HttpClientProviders;

public static class ConnectionClientFactory
{
    /// <summary>
    /// Creates a client for one HTTP(S) server.
    /// The caller owns the client and OAuth is optional.
    /// </summary>
    public static HttpClient Create(
        Uri baseAddress,
        TimeSpan timeout,
        IOAuthClientService? oauthService = null,
        ProxySettings? proxySettings = null
        )
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        if (!baseAddress.IsAbsoluteUri
            || baseAddress.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(baseAddress.UserInfo)
            || !string.IsNullOrEmpty(baseAddress.Query)
            || !string.IsNullOrEmpty(baseAddress.Fragment))
        {
            throw new ArgumentException(
                "An HTTP(S) base URL without credentials, query or fragment is required.",
                nameof(baseAddress)
                );
        }

        var normalizedBaseAddress = new Uri(baseAddress.AbsoluteUri.TrimEnd('/') + "/");

        HttpClient client = Create(
            timeout,
            proxySettings,
            oauthService
            );
        client.BaseAddress = normalizedBaseAddress;

        return client;
    }

    /// <summary>
    /// Creates a caller-owned client for absolute request URLs without setting a base address.
    /// A null proxy configuration preserves the platform's default proxy behavior.
    /// </summary>
    public static HttpClient Create(
        TimeSpan timeout,
        ProxySettings? proxySettings = null,
        IOAuthClientService? oauthService = null
        )
    {
        if (timeout != Timeout.InfiniteTimeSpan
            && (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "The request timeout must be positive and no greater " +
                "than Int32.MaxValue milliseconds, or infinite."
                );
        }

        HttpClient client = new HttpClientBuilder().Create(
            null,
            proxySettings,
            oauthService
            );
        client.Timeout = timeout;

        return client;
    }
}
