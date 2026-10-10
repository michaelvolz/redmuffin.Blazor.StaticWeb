namespace redmuffin.Blazor.StaticWeb.Tests.Support.Http;

/// <summary>
///     HTTP client factory that serves clients backed by a controlled message handler.
/// </summary>
public sealed class HttpClientFactory_Fake : IHttpClientFactory, IDisposable
{
    private readonly HttpMessageHandler _handler;
    private readonly bool _ownsHandler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HttpClientFactory_Fake" /> class
    ///     that serves the supplied handler. The caller keeps ownership of the handler.
    /// </summary>
    /// <param name="handler">Handler that produces every response.</param>
    public HttpClientFactory_Fake(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HttpClientFactory_Fake" /> class
    ///     that wraps the supplied delegate in a controlled handler owned by the factory.
    /// </summary>
    /// <param name="handler">Delegate that produces the response for each request.</param>
    public HttpClientFactory_Fake(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : this(new ControlledHttpHandler_Fake(handler))
    {
        _ownsHandler = true;
    }

    /// <inheritdoc />
    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient(_handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        return client;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHandler)
            _handler.Dispose();
    }
}
