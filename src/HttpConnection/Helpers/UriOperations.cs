namespace HttpConnection.Helpers;

internal static class UriOperations
{
    internal static string BuildUri(
        string endpoint,
        IReadOnlyDictionary<string, string> values
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
            return endpoint;

        string query = string.Join(
            "&",
            values.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")
            );

        int fragmentIndex = endpoint.IndexOf('#');

        if (fragmentIndex < 0)
            return AppendQuery(endpoint, query);

        string endpointWithoutFragment = endpoint[..fragmentIndex];
        string fragment = endpoint[fragmentIndex..];

        return AppendQuery(
            endpointWithoutFragment,
            query
            ) + fragment;
    }

    private static string AppendQuery(
        string endpoint,
        string query
        )
    {
        string separator;

        if (!endpoint.Contains('?'))
            separator = "?";
        else if (endpoint.EndsWith('?') || endpoint.EndsWith('&'))
            separator = "";
        else
            separator = "&";

        return endpoint + separator + query;
    }
}
