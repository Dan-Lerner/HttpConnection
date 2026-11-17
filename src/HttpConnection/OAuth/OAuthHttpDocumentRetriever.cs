using Microsoft.IdentityModel.Protocols;

namespace HttpConnection.OAuth;

internal sealed class OAuthHttpDocumentRetriever :
    IDocumentRetriever
{
    private readonly HttpRestApiClient _httpClient;

    internal OAuthHttpDocumentRetriever(HttpRestApiClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
    }

    public Task<string> GetDocumentAsync(
        string address,
        CancellationToken cancellationToken
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        return _httpClient.GetContentAsync(
            address,
            cancellationToken
            );
    }
}
