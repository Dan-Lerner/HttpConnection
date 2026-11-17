using HttpConnection.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace HttpConnection;

internal sealed class HttpRestApiClient
{
    #region Constants

    private const string CLIENT_NOT_INITIALIZED_MESSAGE =
        "HttpClient is not initialized.";

    private const string REQUEST_TIMEOUT_MESSAGE =
        "HTTP request timed out.";

    #endregion

    #region Fields

    private readonly IHttpClientProvider _clientProvider;
    private readonly IConnectionStatusService? _connectionStatusService;
    private readonly ILogger<HttpRestApiClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion

    #region Constructors

    public HttpRestApiClient(
        IHttpClientProvider clientProvider,
        IConnectionStatusService? connectionStatusService,
        ILogger<HttpRestApiClient>? logger,
        JsonSerializerOptions? jsonOptions
        )
    {
        ArgumentNullException.ThrowIfNull(clientProvider);

        _clientProvider = clientProvider;
        _connectionStatusService = connectionStatusService;
        _logger = logger;
        _jsonOptions = jsonOptions == null
            ? new JsonSerializerOptions(JsonSerializerDefaults.Web)
            : jsonOptions;

        connectionStatusService?.SetOnline(false);
    }

    #endregion

    #region Public methods

    public async Task<string> GetContentAsync(
        string endpoint,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        return await ExecuteAsync(
            (client, token) => SendRequestAsync(
                client,
                HttpMethod.Get,
                endpoint,
                token
                ),
            static (response, token) => response.Content.ReadAsStringAsync(token),
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task<T> GetJsonAsync<T>(
        string endpoint,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        return await ExecuteAsync(
            (client, token) => SendRequestAsync(
                client,
                HttpMethod.Get,
                endpoint,
                token
                ),
            ReadJsonAsync<T>,
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task PostContentAsync(
        string endpoint,
        HttpContent content,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        ArgumentNullException.ThrowIfNull(content);

        await ExecuteAsync(
            (client, token) => SendContentRequestAsync(
                client,
                HttpMethod.Post,
                endpoint,
                content,
                token
                ),
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task<T> PostContentAsync<T>(
        string endpoint,
        HttpContent content,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        ArgumentNullException.ThrowIfNull(content);

        return await ExecuteAsync(
            (client, token) => SendContentRequestAsync(
                client,
                HttpMethod.Post,
                endpoint,
                content,
                token
                ),
            ReadJsonAsync<T>,
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task PutContentAsync(
        string endpoint,
        HttpContent content,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        ArgumentNullException.ThrowIfNull(content);

        await ExecuteAsync(
            (client, token) => SendContentRequestAsync(
                client,
                HttpMethod.Put,
                endpoint,
                content,
                token
                ),
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task<T> PutContentAsync<T>(
        string endpoint,
        HttpContent content,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        ArgumentNullException.ThrowIfNull(content);

        return await ExecuteAsync(
            (client, token) => SendContentRequestAsync(
                client,
                HttpMethod.Put,
                endpoint,
                content,
                token
                ),
            ReadJsonAsync<T>,
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task PostJsonAsync(
        string endpoint,
        object? value,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        await ExecuteAsync(
            (client, token) => SendJsonRequestAsync(
                client,
                HttpMethod.Post,
                endpoint,
                value,
                token
                ),
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task<T> PostJsonAsync<T>(
        string endpoint,
        object? value,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        return await ExecuteAsync(
            (client, token) => SendJsonRequestAsync(
                client,
                HttpMethod.Post,
                endpoint,
                value,
                token
                ),
            ReadJsonAsync<T>,
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task PutJsonAsync(
        string endpoint,
        object? value,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        await ExecuteAsync(
            (client, token) => SendJsonRequestAsync(
                client,
                HttpMethod.Put,
                endpoint,
                value,
                token
                ),
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task<T> PutJsonAsync<T>(
        string endpoint,
        object? value,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        return await ExecuteAsync(
            (client, token) => SendJsonRequestAsync(
                client,
                HttpMethod.Put,
                endpoint,
                value,
                token
                ),
            ReadJsonAsync<T>,
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        string endpoint,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        await ExecuteAsync(
            (client, token) => SendRequestAsync(
                client,
                HttpMethod.Delete,
                endpoint,
                token
                ),
            ct
            )
            .ConfigureAwait(false);
    }

    public async Task<T> DeleteAsync<T>(
        string endpoint,
        CancellationToken ct
        )
    {
        ValidateEndpoint(endpoint);

        return await ExecuteAsync(
            (client, token) => SendRequestAsync(
                client,
                HttpMethod.Delete,
                endpoint,
                token
                ),
            ReadJsonAsync<T>,
            ct
            )
            .ConfigureAwait(false);
    }

    #endregion

    #region Private methods

    private async Task ExecuteAsync(
        Func<HttpClient, CancellationToken, Task<HttpResponseMessage>> sendRequest,
        CancellationToken ct
        )
    {
        await ExecuteAsync(
            sendRequest,
            static (_, _) => Task.FromResult(true),
            ct
            )
            .ConfigureAwait(false);
    }

    private static Task<HttpResponseMessage> SendRequestAsync(
        HttpClient client,
        HttpMethod method,
        string endpoint,
        CancellationToken ct
        )
    {
        return SendRequestAsync(
            client,
            method,
            endpoint,
            null,
            false,
            ct
            );
    }

    private static Task<HttpResponseMessage> SendContentRequestAsync(
        HttpClient client,
        HttpMethod method,
        string endpoint,
        HttpContent content,
        CancellationToken ct
        )
    {
        return SendRequestAsync(
            client,
            method,
            endpoint,
            content,
            true,
            ct
            );
    }

    private async Task<HttpResponseMessage> SendJsonRequestAsync(
        HttpClient client,
        HttpMethod method,
        string endpoint,
        object? value,
        CancellationToken ct
        )
    {
        JsonContent content = JsonContent.Create(
            value,
            null,
            _jsonOptions
            );

        return await SendRequestAsync(
            client,
            method,
            endpoint,
            content,
            false,
            ct
            )
            .ConfigureAwait(false);
    }

    private static async Task<HttpResponseMessage> SendRequestAsync(
        HttpClient client,
        HttpMethod method,
        string endpoint,
        HttpContent? content,
        bool leaveContentOpen,
        CancellationToken ct
        )
    {
        using var request = new HttpRequestMessage(method, endpoint)
        {
            Content = content
        };

        try
        {
            return await client
                .SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                    )
                .ConfigureAwait(false);
        }
        finally
        {
            if (leaveContentOpen)
                request.Content = null;
        }
    }

    private async Task<T> ExecuteAsync<T>(
        Func<HttpClient, CancellationToken, Task<HttpResponseMessage>> sendRequest,
        Func<HttpResponseMessage, CancellationToken, Task<T>> readResponse,
        CancellationToken ct
        )
    {
        HttpClient client = _clientProvider.Client
            ?? throw new InvalidOperationException(CLIENT_NOT_INITIALIZED_MESSAGE);

        try
        {
            using HttpResponseMessage response = await sendRequest(client, ct)
                .ConfigureAwait(false);

            UpdateConnectionStatus(response);

            if (!response.IsSuccessStatusCode)
            {
                string? responseContent = await ReadErrorContentAsync(
                    response,
                    ct
                    )
                    .ConfigureAwait(false);

                HttpRestResponseException exception =
                    HttpRestResponseExceptionFactory.Create<T>(
                        response.StatusCode,
                        response.ReasonPhrase,
                        responseContent,
                        _jsonOptions
                        );

                _logger?.LogWarning(
                    "HTTP request failed with status {StatusCode}: {ReasonPhrase}.",
                    (int)response.StatusCode,
                    response.ReasonPhrase
                    );

                throw exception;
            }

            return await readResponse(
                response,
                ct
                )
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        when (!ct.IsCancellationRequested)
        {
            _connectionStatusService?.SetOnline(false);
            _connectionStatusService?.SetStatusMessage(exception.Message);

            _logger?.LogWarning(exception, REQUEST_TIMEOUT_MESSAGE);

            throw new TimeoutException(
                REQUEST_TIMEOUT_MESSAGE,
                exception
                );
        }
        catch (HttpRequestException exception)
        when (exception is not HttpRestResponseException)
        {
            _connectionStatusService?.SetOnline(false);
            _connectionStatusService?.SetStatusMessage(exception.Message);

            _logger?.LogWarning(exception, "HTTP transport failed.");

            throw;
        }
        catch (IOException exception)
        {
            _connectionStatusService?.SetOnline(false);
            _connectionStatusService?.SetStatusMessage(exception.Message);

            _logger?.LogWarning(
                exception,
                "HTTP response content could not be read."
                );

            throw;
        }
        catch (Exception exception)
        when (exception is JsonException or NotSupportedException)
        {
            _connectionStatusService?.SetStatusMessage(exception.Message);

            _logger?.LogWarning(
                exception,
                "HTTP content could not be converted to the requested JSON type."
                );

            throw;
        }
    }

    private async Task<T> ReadJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken ct
        )
    {
        T? value = await response.Content
            .ReadFromJsonAsync<T>(
                _jsonOptions,
                ct
                )
            .ConfigureAwait(false);

        if (value == null)
        {
            throw new JsonException(
                $"HTTP response content for {typeof(T).FullName} is null."
                );
        }

        return value;
    }

    private static async Task<string?> ReadErrorContentAsync(
        HttpResponseMessage response,
        CancellationToken ct
        )
    {
        if (response.Content.Headers.ContentLength == 0)
            return null;

        string content = await response.Content
            .ReadAsStringAsync(ct)
            .ConfigureAwait(false);

        return content.Length == 0
            ? null
            : content;
    }

    private void UpdateConnectionStatus(HttpResponseMessage response)
    {
        _connectionStatusService?.SetOnline(true);
        _connectionStatusService?.SetStatus(
            response.StatusCode,
            response.ReasonPhrase
            );
    }

    private static void ValidateEndpoint(string endpoint)
        => ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

    #endregion
}
