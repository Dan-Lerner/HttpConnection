using HttpConnection.HttpClientProviders;
using HttpConnection.Interfaces;
using HttpConnection.OAuth.Interfaces;
using HttpConnection.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using XmlTypedSettings;

namespace HttpConnection;

// 1. To add JsonSerializerOptions register it as follows:
//
//services.AddKeyedSingleton<JsonSerializerOptions>(
//    "ConnectionName",
//    (_, _) => new JsonSerializerOptions(JsonSerializerDefaults.Web)
//      {
//          PropertyNameCaseInsensitive = true
//          ...
//      });
//
// 2. To add IConnectionStatusService register it as follows:
//
//services.AddKeyedSingleton<IConnectionStatusService, ConnectionStatusService>(
//    "ConnectionName"
//    );

public static class HttpRestApiClientServiceCollectionExtensions
{
    public static IServiceCollection AddHttpRestApiClient(
        this IServiceCollection services,
        string connectionName,
        ServerSettings defaultServerSettings,
        string proxySettingsSectionName
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        services.AddSettings<ServerSettings>(
            connectionName,
            () => defaultServerSettings
            );
        services.AddSettingsValidator<ServerSettings, ServerSettingsValidator>(
            connectionName);

        services.TryAddSingleton<HttpClientBuilder>();

        services.AddKeyedSingleton<IHttpClientProvider, HttpClientProvider>(
            connectionName,
            (serviceProvider, serviceKey) =>
            {
                string connectionName = GetConnectionName(serviceKey);

                var clientBuilder = serviceProvider
                    .GetRequiredService<HttpClientBuilder>();

                var serverSettings = serviceProvider
                    .GetRequiredKeyedService<ISettings<ServerSettings>>(
                        connectionName
                        );

                var proxySettings = serviceProvider
                    .GetRequiredKeyedService<ISettings<ProxySettings>>(
                        proxySettingsSectionName
                        );

                var oauthService = serviceProvider
                    .GetService<IOAuthClientService>();

                var statusService = serviceProvider
                    .GetKeyedService<IConnectionStatusService>(connectionName);

                return new HttpClientProvider(
                    clientBuilder,
                    serverSettings,
                    proxySettings,
                    oauthService,
                    statusService
                    );
            });

        services.AddKeyedSingleton<HttpRestApiClient>(
            connectionName,
            static (serviceProvider, serviceKey) =>
            {
                string connectionName = GetConnectionName(serviceKey);

                IHttpClientProvider httpClientProvider = serviceProvider
                    .GetRequiredKeyedService<IHttpClientProvider>(connectionName);

                var statusService = serviceProvider
                    .GetKeyedService<IConnectionStatusService>(connectionName);

                var logger = serviceProvider
                    .GetRequiredService<ILogger<HttpRestApiClient>>();

                var jsonOptions = serviceProvider
                    .GetKeyedService<JsonSerializerOptions>(connectionName);

                return new HttpRestApiClient(
                    httpClientProvider,
                    statusService,
                    logger,
                    jsonOptions
                    );
            });

        return services;
    }

    private static string GetConnectionName(object? serviceKey)
    {
        return serviceKey as string
            ?? throw new InvalidOperationException(
                "The HTTP connection service key must be a non-empty string."
                );
    }
}
