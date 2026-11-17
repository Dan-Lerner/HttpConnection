namespace HttpConnection.OAuth.Interfaces;

public interface IOAuthCallbackListener
{
    void Start();

    Task<OAuthCallbackResult> WaitAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken
        );
}
