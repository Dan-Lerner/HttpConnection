using HttpConnection.OAuth.Interfaces;
using HttpConnection.Settings;
using System.Net.Http;

namespace HttpConnection.HttpClientProviders.Interfaces;

internal interface IHttpClientBuilder
{
    HttpClient Create(
        ServerSettings? serverSettings = null,
        ProxySettings? proxySettings = null,
        IOAuthClientService? oauthService = null
        );
}
