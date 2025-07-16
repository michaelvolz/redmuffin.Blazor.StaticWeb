using System.Net;
using System.Text;

namespace redmuffin.Blazor.StaticWeb.Tests.ErrorScenarios;

/// <summary>
///     HTTP message handler that always returns failures for testing error scenarios
/// </summary>
public class FailingHttpMessageHandler : HttpMessageHandler
{
    private readonly string _reasonPhrase;
    private readonly HttpStatusCode _statusCode;

    public FailingHttpMessageHandler(HttpStatusCode statusCode, string reasonPhrase = "Error")
    {
        _statusCode = statusCode;
        _reasonPhrase = reasonPhrase;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            ReasonPhrase = _reasonPhrase,
            RequestMessage = request
        };

        return Task.FromResult(response);
    }
}

/// <summary>
///     HTTP message handler that times out for testing timeout scenarios
/// </summary>
public class TimeoutHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new TaskCanceledException("Request timed out");
    }
}

/// <summary>
///     HTTP message handler that returns success responses for testing
/// </summary>
public class SuccessHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                                        {
                                            "requestId": "test-request-id",
                                            "isSuccess": true,
                                            "results": [
                                                {
                                                    "articleUrl": "https://example.com/article",
                                                    "isSuccess": true,
                                                    "primaryImageUrl": "https://example.com/image.jpg",
                                                    "primaryImageSource": "OpenGraph",
                                                    "extractedImages": [],
                                                    "processingTimeMs": 100,
                                                    "errorMessage": null
                                                }
                                            ],
                                            "totalProcessed": 1,
                                            "successCount": 1,
                                            "failureCount": 0,
                                            "cacheHitCount": 0,
                                            "totalProcessingTimeMs": 100,
                                            "errorMessages": []
                                        }
                                        """, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}

/// <summary>
///     HTTP message handler that returns partial failure responses for testing
/// </summary>
public class PartialFailureHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                                        {
                                            "requestId": "test-request-id",
                                            "isSuccess": true,
                                            "results": [
                                                {
                                                    "articleUrl": "https://example.com/article1",
                                                    "isSuccess": true,
                                                    "primaryImageUrl": "https://example.com/image1.jpg",
                                                    "primaryImageSource": "OpenGraph",
                                                    "extractedImages": [],
                                                    "processingTimeMs": 100,
                                                    "errorMessage": null
                                                },
                                                {
                                                    "articleUrl": "https://example.com/article2",
                                                    "isSuccess": true,
                                                    "primaryImageUrl": "https://example.com/image2.jpg",
                                                    "primaryImageSource": "OpenGraph",
                                                    "extractedImages": [],
                                                    "processingTimeMs": 100,
                                                    "errorMessage": null
                                                }
                                            ],
                                            "totalProcessed": 2,
                                            "successCount": 2,
                                            "failureCount": 0,
                                            "cacheHitCount": 0,
                                            "totalProcessingTimeMs": 200,
                                            "errorMessages": []
                                        }
                                        """, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}