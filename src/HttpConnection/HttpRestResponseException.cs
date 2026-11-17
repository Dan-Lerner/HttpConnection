using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace HttpConnection;

internal class HttpRestResponseException :
    HttpRequestException
{
    internal HttpRestResponseException(
        HttpStatusCode statusCode,
        string? reasonPhrase,
        string? responseContent
        )
        : base(
            CreateMessage(
                statusCode,
                reasonPhrase
                ),
            null,
            statusCode
            )
    {
        ReasonPhrase = reasonPhrase;
        ResponseContent = responseContent;
    }

    internal string? ReasonPhrase { get; }

    internal string? ResponseContent { get; }

    private static string CreateMessage(
        HttpStatusCode statusCode,
        string? reasonPhrase
        )
    {
        return string.IsNullOrWhiteSpace(reasonPhrase)
            ? $"HTTP request failed with status code {(int)statusCode}."
            : $"HTTP request failed with status code {(int)statusCode}: {reasonPhrase}.";
    }
}

internal sealed class HttpRestResponseException<TResponse> :
    HttpRestResponseException
{
    internal HttpRestResponseException(
        HttpStatusCode statusCode,
        string? reasonPhrase,
        string? responseContent,
        TResponse response
        )
        : base(
            statusCode,
            reasonPhrase,
            responseContent
            )
    {
        Response = response;
    }

    internal TResponse Response { get; }
}

internal static class HttpRestResponseExceptionFactory
{
    internal static HttpRestResponseException Create<TResponse>(
        HttpStatusCode statusCode,
        string? reasonPhrase,
        string? responseContent,
        JsonSerializerOptions jsonOptions
        )
    {
        ArgumentNullException.ThrowIfNull(jsonOptions);

        if (responseContent != null)
        {
            try
            {
                TResponse? response = JsonSerializer.Deserialize<TResponse>(
                    responseContent,
                    jsonOptions
                    );

                if (response != null)
                {
                    return new HttpRestResponseException<TResponse>(
                        statusCode,
                        reasonPhrase,
                        responseContent,
                        response
                        );
                }
            }
            catch (Exception exception)
            when (exception is JsonException or NotSupportedException)
            {
            }
        }

        return new HttpRestResponseException(
            statusCode,
            reasonPhrase,
            responseContent
            );
    }
}
