using HttpConnection.OAuth.Interfaces;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HttpConnection.OAuth;

internal sealed class AccessTokenHandler :
    DelegatingHandler
{
    private readonly IOAuthClientService _oauthService;

    internal AccessTokenHandler(IOAuthClientService oauthService)
    {
        _oauthService = oauthService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
        )
    {
        string? token = await _oauthService
            .GetAccessTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token
                );
        }

        HttpResponseMessage response = await base
            .SendAsync(
                request,
                cancellationToken
                )
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(token) &&
            response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _oauthService.RevokeAccessToken(token);
        }

        return response;
    }
}
