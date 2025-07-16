namespace redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

/// <summary>
///     A test HTTP message handler that allows controlling HTTP responses in unit tests
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private Exception? _exception;
    private HttpResponseMessage? _response;

    /// <summary>
    ///     Gets the last HTTP request that was sent through this handler
    /// </summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>
    ///     Gets the content of the last HTTP request as a string
    /// </summary>
    public string? LastRequestContent { get; private set; }

    /// <summary>
    ///     Sets the response that should be returned for HTTP requests
    /// </summary>
    /// <param name="response">The HTTP response to return</param>
    public void SetResponse(HttpResponseMessage response)
    {
        _response = response;
        _exception = null;
    }

    /// <summary>
    ///     Sets an exception that should be thrown for HTTP requests
    /// </summary>
    /// <param name="exception">The exception to throw</param>
    public void SetException(Exception exception)
    {
        _exception = exception;
        _response = null;
    }

    /// <summary>
    ///     Handles the HTTP request and returns the configured response or throws the configured exception
    /// </summary>
    /// <param name="request">The HTTP request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configured HTTP response</returns>
    /// <exception cref="InvalidOperationException">Thrown when no response or exception is configured</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        // Capture request content if available
        if (request.Content != null) LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (_exception != null) throw _exception;

        if (_response != null) return _response;

        throw new InvalidOperationException("No response or exception configured for TestHttpMessageHandler");
    }
}