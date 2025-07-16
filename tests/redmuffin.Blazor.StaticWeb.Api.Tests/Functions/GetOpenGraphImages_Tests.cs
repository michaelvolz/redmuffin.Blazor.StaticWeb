using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using redmuffin.Blazor.StaticWeb.Api.Functions;
using redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public class GetOpenGraphImages_Tests : TestBase
{
    private readonly GetOpenGraphImages _function;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ILogger<GetOpenGraphImages> _logger = Substitute.For<ILogger<GetOpenGraphImages>>();

    public GetOpenGraphImages_Tests()
    {
        _function = new GetOpenGraphImages(_logger, _httpClientFactory);
    }

    private HttpRequest CreateHttpRequest(string jsonBody)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
        request.ContentLength = request.Body.Length;
        return request;
    }

    [Test]
    public async Task RunAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var jsonRequest = "{\"requestId\":\"req-123\",\"articles\":[{\"articleUrl\":\"http://example.com\",\"maxImages\":3}]}";
        var httpRequest = CreateHttpRequest(jsonRequest);

        var httpClient = new HttpClient(new TestHttpMessageHandler()) { BaseAddress = new Uri("http://example.com") };
        _httpClientFactory.CreateClient().Returns(httpClient);

        // Act
        var result = await _function.RunAsync(httpRequest);

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RunAsync_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var jsonRequest = "{\"requestId\":\"req-123\",\"articles\":\"invalid\"}";
        var httpRequest = CreateHttpRequest(jsonRequest);

        // Act
        var result = await _function.RunAsync(httpRequest);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That((result as ObjectResult)?.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task RunAsync_EmptyArticles_ReturnsBadRequest()
    {
        // Arrange
        var jsonRequest = "{\"requestId\":\"req-123\",\"articles\":[]}";
        var httpRequest = CreateHttpRequest(jsonRequest);

        // Act
        var result = await _function.RunAsync(httpRequest);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That((result as ObjectResult)?.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
    }
}