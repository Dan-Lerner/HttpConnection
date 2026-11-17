namespace HttpConnection.Interfaces;

public interface IHttpClientProvider
{
    HttpClient? Client { get; }

    void OnSettingsUpdated();
}
