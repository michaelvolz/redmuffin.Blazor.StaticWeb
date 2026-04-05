using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Extensions;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;
using TUnit;

namespace redmuffin.Blazor.StaticWeb.Common;

public class RaindropItemTests
{
    [Test]
    public async Task Deserialize_WithAllNonNullStrings_SetsPropertiesCorrectly()
    {
        // Arrange
        const string json = """
            {
                "_id": 123,
                "link": "https://example.com",
                "title": "Example Title",
                "excerpt": "Example excerpt",
                "note": "Example note",
                "type": "link",
                "cover": "https://example.com/cover.jpg",
                "domain": "example.com",
                "important": false,
                "removed": false,
                "created": "2023-01-01T00:00:00Z",
                "collectionId": 456,
                "sort": 789
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<RaindropItem>(json);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Link).IsEqualTo("https://example.com");
        await Assert.That(result.Title).IsEqualTo("Example Title");
        await Assert.That(result.Excerpt).IsEqualTo("Example excerpt");
        await Assert.That(result.Note).IsEqualTo("Example note");
        await Assert.That(result.Type).IsEqualTo("link");
        await Assert.That(result.Cover).IsEqualTo("https://example.com/cover.jpg");
        await Assert.That(result.Domain).IsEqualTo("example.com");
    }

    [Test]
    public async Task Deserialize_WithNullStrings_SetsPropertiesToNull()
    {
        // Arrange
        const string json = """
            {
                "_id": 123,
                "link": null,
                "title": null,
                "excerpt": null,
                "note": null,
                "type": null,
                "cover": null,
                "domain": null,
                "important": false,
                "removed": false,
                "created": "2023-01-01T00:00:00Z",
                "collectionId": 456,
                "sort": 789
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<RaindropItem>(json);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Link).IsNull();
        await Assert.That(result.Title).IsNull();
        await Assert.That(result.Excerpt).IsNull();
        await Assert.That(result.Note).IsNull();
        await Assert.That(result.Type).IsNull();
        await Assert.That(result.Cover).IsNull();
        await Assert.That(result.Domain).IsNull();
    }

    [Test]
    public async Task Deserialize_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        const string invalidJson = "{ invalid json }";

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => Task.Run(() => JsonSerializer.Deserialize<RaindropItem>(invalidJson)));
    }

    [Test]
    public async Task ToPruned_WithNullStrings_PreservesNulls()
    {
        // Arrange
        var item = new RaindropItem
        {
            Id = 123,
            Link = null,
            Title = null,
            Excerpt = null,
            Cover = null
        };

        // Act
        var result = item.ToPruned();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(123);
        await Assert.That(result.Link).IsNull();
        await Assert.That(result.Title).IsNull();
        await Assert.That(result.Excerpt).IsNull();
        await Assert.That(result.Cover).IsNull();
    }

    [Test]
    public async Task ToPruned_WithNonNullStrings_PreservesValues()
    {
        // Arrange
        var item = new RaindropItem
        {
            Id = 456,
            Link = "https://example.com",
            Title = "Test Title",
            Excerpt = "Test excerpt",
            Cover = "https://example.com/cover.jpg"
        };

        // Act
        var result = item.ToPruned();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(456);
        await Assert.That(result.Link).IsEqualTo("https://example.com");
        await Assert.That(result.Title).IsEqualTo("Test Title");
        await Assert.That(result.Excerpt).IsEqualTo("Test excerpt");
        await Assert.That(result.Cover).IsEqualTo("https://example.com/cover.jpg");
    }

    [Test]
    public async Task ToFull_FromPruned_CreatesRaindropItem()
    {
        // Arrange
        var pruned = new PrunedRaindropItem
        {
            Id = 789,
            Link = "https://example.com",
            Title = "Pruned Title",
            Excerpt = "Pruned excerpt",
            Cover = "https://example.com/pruned.jpg"
        };

        // Act
        var result = pruned.ToFull();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(789);
        await Assert.That(result.Link).IsEqualTo("https://example.com");
        await Assert.That(result.Title).IsEqualTo("Pruned Title");
        await Assert.That(result.Excerpt).IsEqualTo("Pruned excerpt");
        await Assert.That(result.Cover).IsEqualTo("https://example.com/pruned.jpg");
    }

    [Test]
    public async Task ToPruned_OnCollection_ConvertsAllItems()
    {
        // Arrange
        var items = new List<RaindropItem>
        {
            new() { Id = 1, Link = null, Title = "Title1" },
            new() { Id = 2, Link = "link2", Title = null }
        };

        // Act
        var result = items.ToPruned().ToList();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Id).IsEqualTo(1);
        await Assert.That(result[0].Link).IsNull();
        await Assert.That(result[0].Title).IsEqualTo("Title1");
        await Assert.That(result[1].Id).IsEqualTo(2);
        await Assert.That(result[1].Link).IsEqualTo("link2");
        await Assert.That(result[1].Title).IsNull();
    }

    [Test]
    public async Task ToFull_OnCollection_ConvertsAllItems()
    {
        // Arrange
        var prunedItems = new List<PrunedRaindropItem>
        {
            new() { Id = 1, Link = "link1", Title = "Title1" },
            new() { Id = 2, Link = "link2", Title = "Title2" }
        };

        // Act
        var result = prunedItems.ToFull().ToList();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Id).IsEqualTo(1);
        await Assert.That(result[0].Link).IsEqualTo("link1");
        await Assert.That(result[0].Title).IsEqualTo("Title1");
        await Assert.That(result[1].Id).IsEqualTo(2);
        await Assert.That(result[1].Link).IsEqualTo("link2");
        await Assert.That(result[1].Title).IsEqualTo("Title2");
    }
}