namespace redmuffin.Blazor.StaticWeb.Tests.Support.Http;

/// <summary>
///     HTTP message handler that returns the response produced by a supplied delegate.
/// </summary>
public sealed class ControlledHttpHandler_Fake : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ControlledHttpHandler_Fake" /> class.
    /// </summary>
    /// <param name="handler">Delegate that produces the response for each request.</param>
    public ControlledHttpHandler_Fake(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        return _handler(request);
    }
}
