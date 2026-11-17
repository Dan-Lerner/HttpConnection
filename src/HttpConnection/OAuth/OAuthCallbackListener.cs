using HttpConnection.OAuth.Interfaces;
using System.Net;
using System.Text;

namespace HttpConnection.OAuth;

internal sealed class OAuthCallbackListener :
    IOAuthCallbackListener, 
    IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _redirectUri;

    public OAuthCallbackListener(
        string redirectUri
        )
    {
        _redirectUri = redirectUri;
        _listener.Prefixes.Add(redirectUri);
    }

    public void Start()
    {
        try
        {
            _listener.Start();
        }
        catch (HttpListenerException ex)
        {
            throw new InvalidOperationException(
                $"Error awaiting on callback URI {_redirectUri}.",
                ex
                );
        }
    }

    public async Task<OAuthCallbackResult> WaitAsync(
        TimeSpan timeout,
        CancellationToken ct
        )
    {
        using var timeoutSource = CancellationTokenSource
            .CreateLinkedTokenSource(ct);
        timeoutSource.CancelAfter(timeout);

        HttpListenerContext context;

        try
        {
            context = await _listener
                .GetContextAsync()
                .WaitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The authorization timeout has expired."
                );
        }

        var query = context.Request.QueryString;

        var result = new OAuthCallbackResult(
            Code: query["code"],
            State: query["state"],
            Error: query["error"],
            ErrorDescription: query["error_description"]
            );

        await WriteResponseAsync(
            context.Response,
            result.Error is null
                ? "You are authorized."
                : "Error was occured during authrization."
            );

        return result;
    }

    private static async Task WriteResponseAsync(
        HttpListenerResponse response,
        string message
        )
    {
        var encodedMessage = WebUtility.HtmlEncode(message);

        var bytes = Encoding.UTF8.GetBytes(
            $"""
            <!doctype html>
            <html>
                <head>
                    <meta charset="utf-8">
                </head>
                <body>
                    <h2>{encodedMessage}</h2>
                </body>
            </html>
            """
            );

        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;

        await response.OutputStream.WriteAsync(bytes);

        response.Close();
    }

    public void Dispose()
    {
        _listener.Close();
    }
}
