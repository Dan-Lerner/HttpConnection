using HttpConnection.HttpClientProviders;
using HttpConnection.Interfaces;
using HttpConnection.OAuth.Interfaces;
using HttpConnection.Settings;
using ExampleWPFClient.Infrastructure.SecureStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using XmlTypedSettings;

namespace HttpConnection.OAuth;

public static class OAuthServiceCollectionExtensions
{
    public const string ServiceKey = "OAuthService";

    public static IServiceCollection AddOAuthClient(
        this IServiceCollection services,
        IConfigurationSection optionsSection,
        string? proxySettingsSectionName,
        string? connectionStatusSectionName
        )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(optionsSection);

        services.AddOptions<OAuthOptions>()
            .Bind(optionsSection)
            .Validate(
                static options =>
                    Uri.TryCreate(options.Authority, UriKind.Absolute, out _) &&
                    Uri.TryCreate(options.RedirectUri, UriKind.Absolute, out _) &&
                    !string.IsNullOrWhiteSpace(options.ClientId) &&
                    !string.IsNullOrWhiteSpace(options.Scopes) &&
                    options.AccessTokenSafetyWindowSeconds is >= 0 and <= 300,
                "OAuth configuration is invalid."
                )
            .ValidateOnStart();

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        services.AddSingleton<ITokenStore, TokenStore>(
            (serviceProvider) =>
            {
                var storageFactory = serviceProvider
                    .GetRequiredService<ISecureStorageFactory>();

                var options = serviceProvider
                    .GetRequiredService<IOptions<OAuthOptions>>();

                var timeProvider = serviceProvider
                    .GetRequiredService<TimeProvider>();

                return new TokenStore(
                    storageFactory,
                    options,
                    timeProvider
                    );
            });

        services.TryAddSingleton<HttpClientBuilder>();

        services.AddKeyedSingleton<IHttpClientProvider, HttpClientProvider>(
            ServiceKey,
            (serviceProvider, serviceKey) =>
            {
                var clientBuilder = serviceProvider
                    .GetRequiredService<HttpClientBuilder>();

                var proxySettings = proxySettingsSectionName == null
                    ? null
                    : serviceProvider.GetRequiredKeyedService<ISettings<ProxySettings>>(
                        proxySettingsSectionName
                        );

                return new HttpClientProvider(
                    clientBuilder,
                    null,
                    proxySettings,
                    null,
                    null
                    );
            });

        services.AddKeyedSingleton<HttpRestApiClient>(
            ServiceKey,
            (serviceProvider, serviceKey) =>
            {
                var httpClientProvider = serviceProvider
                    .GetRequiredKeyedService<IHttpClientProvider>(ServiceKey);

                var statusService = connectionStatusSectionName == null
                    ? null
                    : serviceProvider.GetKeyedService<IConnectionStatusService>(
                        connectionStatusSectionName
                        );

                var logger = serviceProvider
                    .GetRequiredService<ILogger<HttpRestApiClient>>();

                var jsonOptions = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                return new HttpRestApiClient(
                    httpClientProvider,
                    statusService,
                    logger,
                    jsonOptions
                    );
            });

        services.AddSingleton<OAuthApiProvider>(
            static (serviceProvider) =>
            {
                var httpClient = serviceProvider
                    .GetRequiredKeyedService<HttpRestApiClient>(ServiceKey);

                var options = serviceProvider
                    .GetRequiredService<IOptions<OAuthOptions>>();

                return new OAuthApiProvider(
                    httpClient,
                    options
                    );
            });

        services.AddSingleton<IOidcTokenValidator, OidcTokenValidator>();

        services.AddSingleton<IOAuthClientService, OAuthClientService>(
            (serviceProvider) =>
            {
                var apiProvider = serviceProvider
                    .GetRequiredService<OAuthApiProvider>();

                var tokenStore = serviceProvider
                    .GetRequiredService<ITokenStore>();

                var tokenValidator = serviceProvider
                    .GetRequiredService<IOidcTokenValidator>();

                var options = serviceProvider
                    .GetRequiredService<IOptions<OAuthOptions>>();

                var localAuthorizationService = serviceProvider
                    .GetService<IOuterLockStatus>();

                var timeProvider = serviceProvider
                    .GetRequiredService<TimeProvider>();

                return new OAuthClientService(
                    apiProvider,
                    tokenStore,
                    tokenValidator,
                    options,
                    localAuthorizationService,
                    timeProvider
                    );
            });

        return services;
    }
}
