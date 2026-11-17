using System.Net;

namespace HttpConnection.Interfaces;

public interface IConnectionStatusService
{
    HttpStatusCode StatusCode { get; }

    string? StatusMessage { get; set; }

    bool Online { get; set; }

    void SetOnline(bool online);

    void SetStatusMessage(string? message);

    void SetStatus(HttpStatusCode statusCode, string? statusMessage);
}
