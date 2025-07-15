using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public class ArticlesApiVerification_Tests : TestBase
{
    [Test]
    public async Task Verify_Articles_API_Response_Structure()
    {
        // Arrange
        var testToken = Configuration["Values:RainDropTestToken"];
        if (string.IsNullOrWhiteSpace(testToken))
        {
            Assert.Fail("RainDropTestToken is null or whitespace. Cannot test actual API response.");
            return;
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", testToken);

        var articlesUrl = "https://api.raindrop.io/rest/v1/raindrops/56658122?sort=-created";

        // Act
        var response = await httpClient.GetAsync(articlesUrl).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(json).IsNotNull();

        // Verify JSON structure
        using var jsonDoc = JsonDocument.Parse(json);

        // Check if response has 'items' property
        await Assert.That(jsonDoc.RootElement.TryGetProperty("items", out var itemsElement)).IsTrue();

        Console.WriteLine("Articles API Response Analysis:");
        Console.WriteLine($"- Status: {response.StatusCode}");
        Console.WriteLine($"- Response Length: {json.Length}");
        Console.WriteLine($"- Items count: {itemsElement.GetArrayLength()}");

        if (itemsElement.GetArrayLength() > 0)
        {
            var firstItem = itemsElement[0];
            Console.WriteLine("First article structure:");
            Console.WriteLine($"  - _id: {(firstItem.TryGetProperty("_id", out var id) ? id.ToString() : "missing")}");
            Console.WriteLine($"  - title: {(firstItem.TryGetProperty("title", out var title) ? title.GetString() : "missing")}");
            Console.WriteLine($"  - type: {(firstItem.TryGetProperty("type", out var itemType) ? itemType.GetString() : "missing")}");
            Console.WriteLine($"  - link: {(firstItem.TryGetProperty("link", out var link) ? link.GetString() : "missing")}");
            Console.WriteLine($"  - domain: {(firstItem.TryGetProperty("domain", out var domain) ? domain.GetString() : "missing")}");
            Console.WriteLine($"  - collectionId: {(firstItem.TryGetProperty("collectionId", out var collectionId) ? collectionId.ToString() : "missing")}");
            Console.WriteLine($"  - created: {(firstItem.TryGetProperty("created", out var created) ? created.GetString() : "missing")}");
            Console.WriteLine(
                $"  - excerpt: {(firstItem.TryGetProperty("excerpt", out var excerpt) ? excerpt.GetString()?.Length > 50 ? excerpt.GetString()?.Substring(0, 50) + "..." : excerpt.GetString() : "missing")}");

            // Test deserialization with existing model
            try
            {
                var articles = JsonSerializer.Deserialize(itemsElement.GetRawText(), RaindropJsonSerializerContext.Default.RaindropItemList);
                await Assert.That(articles).IsNotNull();
                await Assert.That(articles!.Count).IsGreaterThan(0);

                var firstArticle = articles![0];
                Console.WriteLine("Model deserialization test:");
                Console.WriteLine($"  - ID: {firstArticle.Id}");
                Console.WriteLine($"  - Title: {firstArticle.Title}");
                Console.WriteLine($"  - Type: {firstArticle.Type}");
                Console.WriteLine($"  - Domain: {firstArticle.Domain}");
                Console.WriteLine($"  - CollectionId: {firstArticle.CollectionId}");
                Console.WriteLine($"  - Created: {firstArticle.Created}");

                // Verify that the collection ID matches what we expect
                await Assert.That(firstArticle.CollectionId).IsEqualTo(56658122);

                Console.WriteLine("✅ Model deserialization successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Model deserialization failed: {ex.Message}");
                throw;
            }
        }
        else
        {
            Console.WriteLine("⚠️  No articles found in the response");
        }
    }

    [Test]
    public async Task Compare_Videos_And_Articles_API_Structure()
    {
        // Arrange
        var testToken = Configuration["Values:RainDropTestToken"];
        if (string.IsNullOrWhiteSpace(testToken))
        {
            Assert.Fail("RainDropTestToken is null or whitespace. Cannot test actual API response.");
            return;
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", testToken);

        var videosUrl = "https://api.raindrop.io/rest/v1/raindrops/56109697?sort=-created";
        var articlesUrl = "https://api.raindrop.io/rest/v1/raindrops/56658122?sort=-created";

        // Act
        var videosResponse = await httpClient.GetAsync(videosUrl).ConfigureAwait(false);
        var videosJson = await videosResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        var articlesResponse = await httpClient.GetAsync(articlesUrl).ConfigureAwait(false);
        var articlesJson = await articlesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(videosResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(articlesResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var videosDoc = JsonDocument.Parse(videosJson);
        using var articlesDoc = JsonDocument.Parse(articlesJson);

        // Compare structure
        await Assert.That(videosDoc.RootElement.TryGetProperty("items", out var videosItems)).IsTrue();
        await Assert.That(articlesDoc.RootElement.TryGetProperty("items", out var articlesItems)).IsTrue();

        Console.WriteLine("API Structure Comparison:");
        Console.WriteLine("Videos:");
        Console.WriteLine($"  - Status: {videosResponse.StatusCode}");
        Console.WriteLine($"  - Items count: {videosItems.GetArrayLength()}");

        Console.WriteLine("Articles:");
        Console.WriteLine($"  - Status: {articlesResponse.StatusCode}");
        Console.WriteLine($"  - Items count: {articlesItems.GetArrayLength()}");

        if (videosItems.GetArrayLength() > 0 && articlesItems.GetArrayLength() > 0)
        {
            var videoItem = videosItems[0];
            var articleItem = articlesItems[0];

            Console.WriteLine("Structure comparison:");
            Console.WriteLine($"  Videos CollectionId: {(videoItem.TryGetProperty("collectionId", out var vCid) ? vCid.ToString() : "missing")}");
            Console.WriteLine($"  Articles CollectionId: {(articleItem.TryGetProperty("collectionId", out var aCid) ? aCid.ToString() : "missing")}");
            Console.WriteLine($"  Videos Type: {(videoItem.TryGetProperty("type", out var vType) ? vType.GetString() : "missing")}");
            Console.WriteLine($"  Articles Type: {(articleItem.TryGetProperty("type", out var aType) ? aType.GetString() : "missing")}");
            Console.WriteLine($"  Videos Domain: {(videoItem.TryGetProperty("domain", out var vDomain) ? vDomain.GetString() : "missing")}");
            Console.WriteLine($"  Articles Domain: {(articleItem.TryGetProperty("domain", out var aDomain) ? aDomain.GetString() : "missing")}");

            // Verify both can be deserialized with the same model
            try
            {
                var videos = JsonSerializer.Deserialize(videosItems.GetRawText(), RaindropJsonSerializerContext.Default.RaindropItemList);
                var articles = JsonSerializer.Deserialize(articlesItems.GetRawText(), RaindropJsonSerializerContext.Default.RaindropItemList);

                await Assert.That(videos).IsNotNull();
                await Assert.That(articles).IsNotNull();
                await Assert.That(videos!.Count).IsGreaterThan(0);
                await Assert.That(articles!.Count).IsGreaterThan(0);

                Console.WriteLine("✅ Both Videos and Articles can be deserialized with the same RaindropItem model!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Deserialization comparison failed: {ex.Message}");
                throw;
            }
        }
    }
}