using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace ExampleWPFClient.Infrastructure.SecureStore;

[SupportedOSPlatform("windows")]
internal sealed class SecureStorageFactory
    : ISecureStorageFactory
{
    private readonly string _applicationName;
    private readonly string _scopeName;

    public SecureStorageFactory(
        string applicationName,
        string scopeName
        )
    {
        _applicationName = applicationName;
        _scopeName = scopeName;
    }

    public ISecureStorage<T> Create<T>(string identity)
        where T : class
    {
        string userKey = CreateUserStorageKey(identity);

        return new DpapiStorage<T>(
            _applicationName,
            Path.Combine(_scopeName, userKey)
            );
    }

    private static string CreateUserStorageKey(string identity)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(identity);

        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
