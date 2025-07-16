using System.Net;
using System.Text;
using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Enums;
using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly List<HttpRequestMessage> _requests = new();
    private readonly Dictionary<string, HttpResponseMessage> _responses = new();

    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        // Mock the GetOpenGraphImages API response
        if (request.RequestUri?.AbsolutePath == "/api/GetOpenGraphImages")
        {
            var requestContent = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : "";
            var batchRequest = JsonSerializer.Deserialize<BatchImageRequest>(requestContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var response = new BatchImageResponse
            {
                RequestId = batchRequest?.RequestId ?? Guid.NewGuid().ToString(),
                IsSuccess = true,
                Results = batchRequest?.Articles?.Select(article => new ArticleImageResponse
                {
                    ArticleUrl = article.ArticleUrl,
                    IsSuccess = true,
                    PrimaryImageUrl = $"https://example.com/image-{article.ArticleUrl.GetHashCode()}.jpg",
                    PrimaryImageSource = ImageSource.OpenGraph,
                    ExtractedImages = new List<ExtractedImage>(),
                    ProcessingTimeMs = 100,
                    ErrorMessage = null
                }).ToList() ?? new List<ArticleImageResponse>(),
                TotalProcessed = batchRequest?.Articles?.Count ?? 0,
                SuccessCount = batchRequest?.Articles?.Count ?? 0,
                FailureCount = 0,
                CacheHitCount = 0,
                TotalProcessingTimeMs = 500,
                ErrorMessages = new List<string>()
            };

            var json = JsonSerializer.Serialize(response);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        // Default response for other requests
        if (_responses.TryGetValue(request.RequestUri?.ToString() ?? "", out var mockResponse)) return mockResponse;

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    public void SetResponse(string url, HttpResponseMessage response)
    {
        _responses[url] = response;
    }

    public void ClearResponses()
    {
        _responses.Clear();
    }

    public void ClearRequests()
    {
        _requests.Clear();
    }
}