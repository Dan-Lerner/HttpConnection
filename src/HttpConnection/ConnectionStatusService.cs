using HttpConnection.Interfaces;
using System.Net;

namespace HttpConnection;

internal class ConnectionStatusService :
    IConnectionStatusService
{
    public HttpStatusCode StatusCode { get; set; }

    public string? StatusMessage { get; set; }
    
    public bool Online { get; set; }

    public void SetOnline(bool online)
    {
        Online = online;
    }
    
    public void SetStatus(HttpStatusCode statusCode, string? statusMessage)
    {
        StatusCode = statusCode;
    }
    
    public void SetStatusMessage(string? message)
    {
        StatusMessage = message;
    }
}
