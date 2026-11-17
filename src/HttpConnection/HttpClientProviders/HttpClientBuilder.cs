using HttpConnection.HttpClientProviders.Interfaces;
using HttpConnection.OAuth;
using HttpConnection.OAuth.Interfaces;
using HttpConnection.Settings;
using System.Net;
using System.Net.Http.Headers;

namespace HttpConnection.HttpClientProviders;

internal sealed class HttpClientBuilder : 
    IHttpClientBuilder
{
    public HttpClient Create(
        ServerSettings? serverSettings,
        ProxySettings? proxySettings,
        IOAuthClientService? oauthService
        )
    {
        if (!ProxySettingsValidator.TryValidate(
            proxySettings,
            out string? proxyError
            ))
        {
            throw new ArgumentException(
                proxyError,
                nameof(proxySettings)
                );
        }

        HttpMessageHandler handler = CreateTransportHandler(proxySettings);

        if (oauthService != null)
        {
            handler = new AccessTokenHandler(oauthService)
            {
                InnerHandler = handler
            };
        }

        var client = new HttpClient(
            handler,
            disposeHandler: true
            );

        if (serverSettings != null)
        {
            client.Timeout = TimeSpan.FromSeconds(serverSettings.TimeOutSeconds);

            client.BaseAddress = CreateBaseAddress(serverSettings);
        }

        InitializeDefaultHeaders(client);

        return client;
    }

    private static HttpClientHandler CreateTransportHandler(
        ProxySettings? proxySettings
        )
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
        };

        if (proxySettings == null)
            return handler;

        handler.UseProxy = proxySettings.UseProxy;

        if (!handler.UseProxy)
            return handler;

        if (proxySettings.UseSystemProxy)
        {
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

            return handler;
        }

        var proxy = new WebProxy(
            proxySettings.Host,
            proxySettings.Port
            );

        if (!string.IsNullOrWhiteSpace(proxySettings.User) &&
            !string.IsNullOrWhiteSpace(proxySettings.Password))
        {
            proxy.Credentials = new NetworkCredential(
                proxySettings.User,
                proxySettings.Password
                );
        }

        handler.Proxy = proxy;

        return handler;
    }

    private static Uri CreateBaseAddress(
        ServerSettings serverSettings
        )
    {
        var builder = new UriBuilder(
            serverSettings.IsSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
            serverSettings.Host,
            serverSettings.Port
            );

        if (!string.IsNullOrWhiteSpace(serverSettings.ApiPath))
            builder.Path = $"{serverSettings.ApiPath.Trim('/')}/";

        return builder.Uri;
    }

    private static void InitializeDefaultHeaders(
        HttpClient client
        )
    {
        client.DefaultRequestHeaders.Accept.Clear();

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
            );
    }
}
