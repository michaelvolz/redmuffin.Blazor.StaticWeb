using System.Net.Http.Headers;
using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

[Category("Feature:Api")]
public sealed partial class ArticlesApiVerification_Tests
{
    /// <summary>
    ///     Validates that Articles can be properly deserialized and maintain expected collection ID.
    /// </summary>
    [Test]
    public async Task Should_Deserialize_Articles_With_Correct_Collection_ID_When_API_Responds()
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
        var response = await httpClient.GetAsync(articlesUrl).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        using var jsonDoc = JsonDocument.Parse(json);
        jsonDoc.RootElement.TryGetProperty("items", out var itemsElement);

        var articles = JsonSerializer.Deserialize<List<RaindropItem>>(itemsElement.GetRawText(), RaindropJsonSerializerContext.DefaultOptions);

        // Assert
        await Assert.That(articles).IsNotNull();
        await Assert.That(articles!.Count).IsGreaterThan(0);

        var firstArticle = articles![0];
        await Assert.That(firstArticle.CollectionId).IsEqualTo(56658122);
        await Assert.That(firstArticle.Id).IsGreaterThan(0L);
        await Assert.That(firstArticle.Title).IsNotNull();
    }

    /// <summary>
    ///     Validates that both Videos and Articles can be deserialized using the same RaindropItem model.
    /// </summary>
    [Test]
    public async Task Should_Deserialize_With_Same_Model_When_Processing_Videos_And_Articles()
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
        var videosResponse = await httpClient.GetAsync(videosUrl).ConfigureAwait(false);
        var videosJson = await videosResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        var articlesResponse = await httpClient.GetAsync(articlesUrl).ConfigureAwait(false);
        var articlesJson = await articlesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        using var videosDoc = JsonDocument.Parse(videosJson);
        using var articlesDoc = JsonDocument.Parse(articlesJson);

        videosDoc.RootElement.TryGetProperty("items", out var videosItems);
        articlesDoc.RootElement.TryGetProperty("items", out var articlesItems);

        // Assert
        var videos = JsonSerializer.Deserialize<List<RaindropItem>>(videosItems.GetRawText(), RaindropJsonSerializerContext.DefaultOptions);
        var articles = JsonSerializer.Deserialize<List<RaindropItem>>(articlesItems.GetRawText(), RaindropJsonSerializerContext.DefaultOptions);

        await Assert.That(videos).IsNotNull();
        await Assert.That(articles).IsNotNull();
        await Assert.That(videos!.Count).IsGreaterThan(0);
        await Assert.That(articles!.Count).IsGreaterThan(0);
    }
}