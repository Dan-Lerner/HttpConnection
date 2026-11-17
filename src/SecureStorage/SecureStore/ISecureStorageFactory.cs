namespace ExampleWPFClient.Infrastructure.SecureStore;

public interface ISecureStorageFactory
{
    ISecureStorage<T> Create<T>(string user)
        where T : class;
}
