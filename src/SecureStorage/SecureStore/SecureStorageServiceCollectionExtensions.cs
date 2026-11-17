using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Versioning;

namespace ExampleWPFClient.Infrastructure.SecureStore;

public static class SecureStorageServiceCollectionExtensions
{
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddSecureStorage(
        this IServiceCollection services,
        string applicationName,
        string scopeName
        )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeName);

        services.AddSingleton<ISecureStorageFactory>(
            _ => new SecureStorageFactory(
                applicationName,
                scopeName
                ));

        return services;
    }
}
