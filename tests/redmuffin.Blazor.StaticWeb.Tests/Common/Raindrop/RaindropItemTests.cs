using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Extensions;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Models;
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

    // Characterization tests for IsValid() — all decision paths
    public sealed class IsValidTests
    {
        [Test]
        public async Task ReturnsFalse_When_IdIsZero()
        {
            var item = new PrunedRaindropItem { Id = 0 };
            await Assert.That(item.IsValid()).IsFalse();
        }

        [Test]
        public async Task ReturnsFalse_When_IdIsNegative()
        {
            var item = new PrunedRaindropItem { Id = -1 };
            await Assert.That(item.IsValid()).IsFalse();
        }

        [Test]
        public async Task ReturnsTrue_When_AllFieldsValid()
        {
            var item = new PrunedRaindropItem
            {
                Id = 1,
                Link = "https://example.com",
                Cover = "https://example.com/cover.jpg",
                Title = "Valid Title",
                Excerpt = "Valid excerpt"
            };
            await Assert.That(item.IsValid()).IsTrue();
        }

        [Test]
        public async Task ReturnsTrue_When_AllOptionalFieldsAreNull()
        {
            var item = new PrunedRaindropItem { Id = 1 };
            await Assert.That(item.IsValid()).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_When_LinkIsInvalidUri()
        {
            var item = new PrunedRaindropItem { Id = 1, Link = "not-a-uri" };
            await Assert.That(item.IsValid()).IsFalse();
        }

        [Test]
        public async Task ReturnsTrue_When_LinkIsValidUri()
        {
            var item = new PrunedRaindropItem { Id = 1, Link = "https://example.com" };
            await Assert.That(item.IsValid()).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_When_CoverIsInvalidUri()
        {
            var item = new PrunedRaindropItem { Id = 1, Cover = "not-a-uri" };
            await Assert.That(item.IsValid()).IsFalse();
        }

        [Test]
        public async Task ReturnsTrue_When_CoverIsValidUri()
        {
            var item = new PrunedRaindropItem { Id = 1, Cover = "https://example.com/img.jpg" };
            await Assert.That(item.IsValid()).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_When_TitleExceeds500Characters()
        {
            var item = new PrunedRaindropItem { Id = 1, Title = new string('x', 501) };
            await Assert.That(item.IsValid()).IsFalse();
        }

        [Test]
        public async Task ReturnsTrue_When_TitleIsExactly500Characters()
        {
            var item = new PrunedRaindropItem { Id = 1, Title = new string('x', 500) };
            await Assert.That(item.IsValid()).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_When_ExcerptExceeds2000Characters()
        {
            var item = new PrunedRaindropItem { Id = 1, Excerpt = new string('x', 2001) };
            await Assert.That(item.IsValid()).IsFalse();
        }

        [Test]
        public async Task ReturnsTrue_When_ExcerptIsExactly2000Characters()
        {
            var item = new PrunedRaindropItem { Id = 1, Excerpt = new string('x', 2000) };
            await Assert.That(item.IsValid()).IsTrue();
        }
    }

    public sealed class ValidateOrThrowTests
    {
        [Test]
        public async Task DoesNotThrow_When_AllFieldsValid()
        {
            var item = new PrunedRaindropItem
            {
                Id = 1,
                Link = "https://example.com",
                Title = "Valid",
                Excerpt = "Valid"
            };
            await Assert.That(() => item.ValidateOrThrow()).ThrowsNothing();
        }

        [Test]
        public async Task Throws_When_IdIsZero()
        {
            var item = new PrunedRaindropItem { Id = 0 };
            await Assert.That(() => item.ValidateOrThrow())
                .Throws<ValidationException>()
                .WithMessage("ID must be a positive value.");
        }

        [Test]
        public async Task Throws_When_LinkIsInvalid()
        {
            var item = new PrunedRaindropItem { Id = 1, Link = "not-a-uri" };
            await Assert.That(() => item.ValidateOrThrow())
                .Throws<ValidationException>()
                .WithMessage("Link must be a valid absolute URI.");
        }

        [Test]
        public async Task Throws_When_CoverIsInvalid()
        {
            var item = new PrunedRaindropItem { Id = 1, Cover = "not-a-uri" };
            await Assert.That(() => item.ValidateOrThrow())
                .Throws<ValidationException>()
                .WithMessage("Cover must be a valid absolute URI.");
        }

        [Test]
        public async Task Throws_When_TitleExceeds500Chars()
        {
            var item = new PrunedRaindropItem { Id = 1, Title = new string('x', 501) };
            await Assert.That(() => item.ValidateOrThrow())
                .Throws<ValidationException>()
                .WithMessage("Title cannot exceed 500 characters.");
        }

        [Test]
        public async Task Throws_When_ExcerptExceeds2000Chars()
        {
            var item = new PrunedRaindropItem { Id = 1, Excerpt = new string('x', 2001) };
            await Assert.That(() => item.ValidateOrThrow())
                .Throws<ValidationException>()
                .WithMessage("Excerpt cannot exceed 2000 characters.");
        }
    }
}