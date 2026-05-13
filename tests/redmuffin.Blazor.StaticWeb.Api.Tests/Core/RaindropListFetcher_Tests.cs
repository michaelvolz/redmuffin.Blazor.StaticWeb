using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Core;

[Category("Feature:Api")]
public sealed class RaindropListFetcher_Tests
{
    /// <summary>
    ///     Validates that when the Raindrop API returns HTTP 200 with an "items"
    ///     property, the fetcher extracts and returns only the items array.
    /// </summary>
    [Test]
    public async Task Should_Return_Items_When_Api_Returns_Success_With_Items()
    {
        // Arrange
        var collectionId = "12345";
        var bearerToken = "test-token";
        var apiJson = """{"items": [{"id": 1, "title": "Test Article"}], "count": 1}""";

        using var handler = new ControlledHttpHandler_Fake(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(apiJson)
            }));
        var factory = new HttpClientFactory_Fake(handler);

        var functionContext = RaindropListArticles_Tests.TestScope.CreateFunctionContext("TestFunction");
        var request = RaindropListArticles_Tests.TestScope.CreateHttpRequestData(functionContext);

        string? capturedLogUrl = null;
        using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace));

        // Act
        using var response = (RaindropListArticles_Tests.HttpResponseData_Mock)await RaindropListFetcher.FetchAsync(
            request, collectionId, bearerToken, factory, loggerFactory.CreateLogger("Test"),
            logFetch: (_, url) => capturedLogUrl = url,
            logError: (_, _) => { },
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(capturedLogUrl).Contains(collectionId);
        var body = response.GetBodyAsString();
        using var doc = JsonDocument.Parse(body);
        await Assert.That(doc.RootElement.GetArrayLength()).IsEqualTo(1);
        await Assert.That(doc.RootElement[0].GetProperty("id").GetInt32()).IsEqualTo(1);
        await Assert.That(doc.RootElement[0].GetProperty("title").GetString()).IsEqualTo("Test Article");
    }

    /// <summary>
    ///     Validates that when the API returns HTTP 200 but the JSON lacks an
    ///     "items" property, the fetcher falls back to returning the full JSON
    ///     body as a defensive measure against unexpected API format changes.
    /// </summary>
    [Test]
    public async Task Should_Return_Full_Json_When_Items_Property_Missing()
    {
        // Arrange
        var apiJson = """{"not_items": "something_else"}""";
        using var handler = new ControlledHttpHandler_Fake(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(apiJson)
            }));
        var factory = new HttpClientFactory_Fake(handler);
        using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace));

        var functionContext = RaindropListArticles_Tests.TestScope.CreateFunctionContext("TestFunction");
        var request = RaindropListArticles_Tests.TestScope.CreateHttpRequestData(functionContext);

        // Act
        using var response = (RaindropListArticles_Tests.HttpResponseData_Mock)await RaindropListFetcher.FetchAsync(
            request, "cid", "token", factory, loggerFactory.CreateLogger("Test"),
            logFetch: (_, _) => { },
            logError: (_, _) => { },
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = response.GetBodyAsString();
        await Assert.That(body).Contains("not_items");
    }

    /// <summary>
    ///     Validates that non-success HTTP status codes from the Raindrop API
    ///     are converted to HTTP 400 with a structured error body.
    /// </summary>
    [Test]
    public async Task Should_Return_BadRequest_When_Api_Returns_Error()
    {
        // Arrange
        using var handler = new ControlledHttpHandler_Fake(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"error": "invalid_token"}""")
            }));
        var factory = new HttpClientFactory_Fake(handler);
        using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace));

        var functionContext = RaindropListArticles_Tests.TestScope.CreateFunctionContext("TestFunction");
        var request = RaindropListArticles_Tests.TestScope.CreateHttpRequestData(functionContext);

        // Act
        using var response = (RaindropListArticles_Tests.HttpResponseData_Mock)await RaindropListFetcher.FetchAsync(
            request, "cid", "token", factory, loggerFactory.CreateLogger("Test"),
            logFetch: (_, _) => { },
            logError: (_, _) => { },
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var body = response.GetBodyAsString();
        await Assert.That(body).Contains("invalid_token");
        JsonDocument.Parse(body); // Verify response is valid JSON
    }

    /// <summary>
    ///     Validates that <see cref="OperationCanceledException" /> is NOT
    ///     swallowed — it propagates to the Functions host.
    /// </summary>
    [Test]
    public async Task Should_Throw_OperationCanceledException_When_Canceled()
    {
        // Arrange
        using var handler = new ControlledHttpHandler_Fake(_ =>
            throw new OperationCanceledException("Request canceled"));
        var factory = new HttpClientFactory_Fake(handler);
        using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace));

        var functionContext = RaindropListArticles_Tests.TestScope.CreateFunctionContext("TestFunction");
        var request = RaindropListArticles_Tests.TestScope.CreateHttpRequestData(functionContext);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await RaindropListFetcher.FetchAsync(
                request, "cid", "token", factory, loggerFactory.CreateLogger("Test"),
                logFetch: (_, _) => { },
                logError: (_, _) => { },
                cancellationToken: CancellationToken.None).ConfigureAwait(false));
    }

    /// <summary>
    ///     Validates that unexpected exceptions (network failure, DNS, invalid
    ///     JSON) are caught and converted to HTTP 500 with the exception message.
    /// </summary>
    [Test]
    [Arguments("Simulated network failure")]
    [Arguments("DNS resolution timeout")]
    [Arguments("Invalid JSON in response body")]
    public async Task Should_Return_InternalServerError_When_Exception_Occurs(string errorMessage)
    {
        // Arrange
        using var handler = new ControlledHttpHandler_Fake(_ =>
            throw new HttpRequestException(errorMessage));
        var factory = new HttpClientFactory_Fake(handler);
        using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace));

        var functionContext = RaindropListArticles_Tests.TestScope.CreateFunctionContext("TestFunction");
        var request = RaindropListArticles_Tests.TestScope.CreateHttpRequestData(functionContext);

        Exception? capturedException = null;

        // Act
        using var response = (RaindropListArticles_Tests.HttpResponseData_Mock)await RaindropListFetcher.FetchAsync(
            request, "cid", "token", factory, loggerFactory.CreateLogger("Test"),
            logFetch: (_, _) => { },
            logError: (_, ex) => capturedException = ex,
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        await Assert.That(capturedException).IsNotNull();
        var body = response.GetBodyAsString();
        await Assert.That(body).Contains(errorMessage);
        JsonDocument.Parse(body); // Verify response is valid JSON even in error state
    }

    // ── Test infrastructure ────────────────────────────────────────

    private sealed class HttpClientFactory_Fake(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class ControlledHttpHandler_Fake : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public ControlledHttpHandler_Fake(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
