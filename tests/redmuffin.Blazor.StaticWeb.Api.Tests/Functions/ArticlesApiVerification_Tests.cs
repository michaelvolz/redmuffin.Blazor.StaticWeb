using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

/// <summary>
///     Validates API structure compatibility between Videos and Articles endpoints.
///     Ensures consistent data models and proper deserialization across different Raindrop collections.
/// </summary>
[Category("Feature:Api")]
[Category("Integration")]
public sealed partial class ArticlesApiVerification_Tests
{
    private static readonly HttpStatusCode[] TransientStatuses = [HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout];

    private static async Task<(HttpResponseMessage Response, string Content)> GetWithRetryAsync(
        HttpClient client, string url, CancellationToken cancellationToken, int maxRetries = 3)
    {
        // Immediate retries for transient statuses; wall-clock backoff removed (SW004).
        // Whole-test flakiness is handled by TUnit [Retry] on callers.
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!TransientStatuses.Contains(response.StatusCode))
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return (response, content);
            }
        }

        var finalResponse = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        var finalContent = await finalResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return (finalResponse, finalContent);
    }

    /// <summary>
    ///     Validates that Articles API response maintains expected structure and data integrity.
    /// </summary>
    [Test]
    [Timeout(30_000)]
    [Retry(2)]
    public async Task Should_Maintain_Expected_Structure_When_Articles_API_Responds(CancellationToken cancellationToken)
    {
        // Arrange
        using var scope = CreateTestScope();
        var testToken = scope.Configuration["Values:RainDropTestToken"];

        if (string.IsNullOrWhiteSpace(testToken))
        {
            Assert.Fail("RainDropTestToken is null or whitespace. Cannot test actual API response.");
            return;
        }

        using var httpClient = TestScope.CreateHttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", testToken);

        var articlesUrl = "https://api.raindrop.io/rest/v1/raindrops/56658122?sort=-created";

        // Act
        var (response, json) = await GetWithRetryAsync(httpClient, articlesUrl, cancellationToken).ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(json).IsNotNull();

        using var jsonDoc = JsonDocument.Parse(json);
        await Assert.That(jsonDoc.RootElement.TryGetProperty("items", out var itemsElement)).IsTrue();
        await Assert.That(itemsElement.GetArrayLength()).IsGreaterThan(0);
    }

    /// <summary>
    ///     Validates that Videos and Articles APIs maintain structural compatibility for unified processing.
    /// </summary>
    [Test]
    [Timeout(60_000)]
    [Retry(2)]
    public async Task Should_Maintain_Structural_Compatibility_When_Comparing_Videos_And_Articles_APIs(CancellationToken cancellationToken)
    {
        // Arrange
        using var scope = CreateTestScope();
        var testToken = scope.Configuration["Values:RainDropTestToken"];

        if (string.IsNullOrWhiteSpace(testToken))
        {
            Assert.Fail("RainDropTestToken is null or whitespace. Cannot test actual API response.");
            return;
        }

        using var httpClient = TestScope.CreateHttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", testToken);

        var videosUrl = "https://api.raindrop.io/rest/v1/raindrops/56109697?sort=-created";
        var articlesUrl = "https://api.raindrop.io/rest/v1/raindrops/56658122?sort=-created";

        // Act
        var (videosResponse, videosJson) = await GetWithRetryAsync(httpClient, videosUrl, cancellationToken).ConfigureAwait(false);
        var (articlesResponse, articlesJson) = await GetWithRetryAsync(httpClient, articlesUrl, cancellationToken).ConfigureAwait(false);

        // Assert
        await Assert.That(videosResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(articlesResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var videosDoc = JsonDocument.Parse(videosJson);
        using var articlesDoc = JsonDocument.Parse(articlesJson);

        await Assert.That(videosDoc.RootElement.TryGetProperty("items", out var videosItems)).IsTrue();
        await Assert.That(articlesDoc.RootElement.TryGetProperty("items", out var articlesItems)).IsTrue();

        await Assert.That(videosItems.GetArrayLength()).IsGreaterThan(0);
        await Assert.That(articlesItems.GetArrayLength()).IsGreaterThan(0);
    }
}