using HttpConnection.HttpClientProviders.Interfaces;
using HttpConnection.Interfaces;
using HttpConnection.OAuth.Interfaces;
using HttpConnection.Settings;
using System.Net.Http;
using XmlTypedSettings;

namespace HttpConnection.HttpClientProviders;

internal sealed class HttpClientProvider
    : IHttpClientProvider
{
    #region Fields

    private readonly IHttpClientBuilder _clientBuilder;
    private readonly ISettings<ServerSettings>? _serverSettings;
    private readonly ISettings<ProxySettings>? _proxySettings;
    private readonly IOAuthClientService? _oauthService;
    private readonly IConnectionStatusService? _connectionStatusService;

    HttpClient _client;

    #endregion

    #region Constructors

    public HttpClientProvider(
        IHttpClientBuilder clientBuilder,
        ISettings<ServerSettings>? serverSettings,
        ISettings<ProxySettings>? proxySettings,
        IOAuthClientService? oauthService,
        IConnectionStatusService? connectionStatusService
        )
    {
        _clientBuilder = clientBuilder;
        _serverSettings = serverSettings;
        _proxySettings = proxySettings;
        _oauthService = oauthService;
        _connectionStatusService = connectionStatusService;

        if (_connectionStatusService != null)
            _connectionStatusService.Online = false;

        _client = _clientBuilder.Create(
            _serverSettings?.Value,
            _proxySettings?.Value,
            _oauthService
            );
    }

    #endregion

    #region Properties

    public ServerSettings? ServerSettings
        => _serverSettings?.Value;

    public ProxySettings? ProxySettings
        => _proxySettings?.Value;

    #endregion

    #region Methods

    #endregion

    #region IHttpClientProvider Implementation 

    public HttpClient Client
        => Volatile.Read(ref _client);

    public void OnSettingsUpdated()
        => ReplaceClient();

    #endregion

    #region Private methods

    private void ReplaceClient()
    {
        if (_connectionStatusService != null)
            _connectionStatusService.Online = false;

        var newClient = _clientBuilder.Create(
            _serverSettings?.Value,
            _proxySettings?.Value,
            _oauthService
            );

        HttpClient oldClient = Interlocked.Exchange(ref _client, newClient);

        // !!! Potential problem
        oldClient.Dispose();
    }

    #endregion
}
