namespace ExampleWPFClient.Infrastructure.SecureStore;

public interface ISecureStorage<T>
{
    Task<T?> LoadAsync(CancellationToken ct);

    Task SaveAsync(T value, CancellationToken ct);

    Task ClearAsync(CancellationToken ct);
}
