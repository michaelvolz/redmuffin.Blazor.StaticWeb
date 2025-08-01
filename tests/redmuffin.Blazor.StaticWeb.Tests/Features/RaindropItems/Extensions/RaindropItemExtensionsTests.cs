using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Extensions;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.RaindropItems.Extensions;

/// <summary>
///     Unit tests for RaindropItemExtensions conversion methods.
///     Tests the conversion between RaindropItem and PrunedRaindropItem models.
/// </summary>
public partial class RaindropItemExtensionsTests
{
    [Test]
    public async Task RoundTrip_Collection_ToPrunedAndToFull_PreservesEssentialData()
    {
        // Arrange
        var originalItems = CreateTestRaindropItemCollection();

        // Act
        var prunedItems = originalItems.ToPruned().ToList();
        var roundTripItems = prunedItems.ToFull().ToList();

        // Assert
        await Assert.That(roundTripItems).HasCount().EqualTo(originalItems.Count);

        for (var i = 0; i < originalItems.Count; i++)
        {
            await Assert.That(roundTripItems[i].Id).IsEqualTo(originalItems[i].Id);
            await Assert.That(roundTripItems[i].Link).IsEqualTo(originalItems[i].Link);
            await Assert.That(roundTripItems[i].Title).IsEqualTo(originalItems[i].Title);
            await Assert.That(roundTripItems[i].Excerpt).IsEqualTo(originalItems[i].Excerpt);
            await Assert.That(roundTripItems[i].Cover).IsEqualTo(originalItems[i].Cover);
        }
    }

    [Test]
    public async Task RoundTrip_ToPrunedAndToFull_PreservesEssentialData()
    {
        // Arrange
        var originalItem = CreateTestRaindropItem();

        // Act
        var prunedItem = originalItem.ToPruned();
        var roundTripItem = prunedItem.ToFull();

        // Assert
        await Assert.That(roundTripItem.Id).IsEqualTo(originalItem.Id);
        await Assert.That(roundTripItem.Link).IsEqualTo(originalItem.Link);
        await Assert.That(roundTripItem.Title).IsEqualTo(originalItem.Title);
        await Assert.That(roundTripItem.Excerpt).IsEqualTo(originalItem.Excerpt);
        await Assert.That(roundTripItem.Cover).IsEqualTo(originalItem.Cover);
    }

    [Test]
    public async Task ToFull_Collection_EmptyCollection_ReturnsEmptyCollection()
    {
        // Arrange
        var emptyCollection = new List<PrunedRaindropItem>();

        // Act
        var fullItems = emptyCollection.ToFull().ToList();

        // Assert
        await Assert.That(fullItems).IsEmpty();
    }

    [Test]
    public async Task ToFull_Collection_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<PrunedRaindropItem>? nullCollection = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var _ = nullCollection!.ToFull().ToList();
        });
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ToFull_Collection_ValidItems_ReturnsCorrectRaindropItems()
    {
        // Arrange
        var prunedItems = CreateTestPrunedRaindropItemCollection();

        // Act
        var fullItems = prunedItems.ToFull().ToList();

        // Assert
        await Assert.That(fullItems).HasCount().EqualTo(3);

        for (var i = 0; i < prunedItems.Count; i++)
        {
            await Assert.That(fullItems[i].Id).IsEqualTo(prunedItems[i].Id);
            await Assert.That(fullItems[i].Link).IsEqualTo(prunedItems[i].Link);
            await Assert.That(fullItems[i].Title).IsEqualTo(prunedItems[i].Title);
            await Assert.That(fullItems[i].Excerpt).IsEqualTo(prunedItems[i].Excerpt);
            await Assert.That(fullItems[i].Cover).IsEqualTo(prunedItems[i].Cover);
        }
    }

    [Test]
    public async Task ToFull_NullPrunedItem_ThrowsArgumentNullException()
    {
        // Arrange
        PrunedRaindropItem? nullItem = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => nullItem!.ToFull());
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ToFull_PrunedItemWithEmptyStrings_PreservesEmptyValues()
    {
        // Arrange
        var prunedItem = new PrunedRaindropItem
        {
            Id = 789,
            Link = string.Empty,
            Title = string.Empty,
            Excerpt = string.Empty,
            Cover = string.Empty
        };

        // Act
        var fullItem = prunedItem.ToFull();

        // Assert
        await Assert.That(fullItem).IsNotNull();
        await Assert.That(fullItem.Id).IsEqualTo(789);
        await Assert.That(fullItem.Link).IsEqualTo(string.Empty);
        await Assert.That(fullItem.Title).IsEqualTo(string.Empty);
        await Assert.That(fullItem.Excerpt).IsEqualTo(string.Empty);
        await Assert.That(fullItem.Cover).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ToFull_ValidPrunedItem_ReturnsCorrectRaindropItem()
    {
        // Arrange
        var prunedItem = CreateTestPrunedRaindropItem();

        // Act
        var fullItem = prunedItem.ToFull();

        // Assert
        await Assert.That(fullItem).IsNotNull();
        await Assert.That(fullItem.Id).IsEqualTo(prunedItem.Id);
        await Assert.That(fullItem.Link).IsEqualTo(prunedItem.Link);
        await Assert.That(fullItem.Title).IsEqualTo(prunedItem.Title);
        await Assert.That(fullItem.Excerpt).IsEqualTo(prunedItem.Excerpt);
        await Assert.That(fullItem.Cover).IsEqualTo(prunedItem.Cover);

        // Verify non-essential fields have default values
        await Assert.That(fullItem.Note).IsEqualTo(string.Empty);
        await Assert.That(fullItem.Type).IsEqualTo(string.Empty);
        await Assert.That(fullItem.Domain).IsEqualTo(string.Empty);
        await Assert.That(fullItem.Important).IsFalse();
        await Assert.That(fullItem.Removed).IsFalse();
        await Assert.That(fullItem.Created).IsEqualTo(DateTime.MinValue);
        await Assert.That(fullItem.CollectionId).IsEqualTo(0);
        await Assert.That(fullItem.Sort).IsEqualTo(0);
        await Assert.That(fullItem.Tags).IsEmpty();
        await Assert.That(fullItem.Media).IsEmpty();
        await Assert.That(fullItem.Highlights).IsEmpty();
    }

    [Test]
    public async Task ToPruned_Collection_EmptyCollection_ReturnsEmptyCollection()
    {
        // Arrange
        var emptyCollection = new List<RaindropItem>();

        // Act
        var prunedItems = emptyCollection.ToPruned().ToList();

        // Assert
        await Assert.That(prunedItems).IsEmpty();
    }

    [Test]
    public async Task ToPruned_Collection_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<RaindropItem>? nullCollection = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var _ = nullCollection!.ToPruned().ToList();
        });
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ToPruned_Collection_ValidItems_ReturnsCorrectPrunedItems()
    {
        // Arrange
        var raindropItems = CreateTestRaindropItemCollection();

        // Act
        var prunedItems = raindropItems.ToPruned().ToList();

        // Assert
        await Assert.That(prunedItems).HasCount().EqualTo(3);

        for (var i = 0; i < raindropItems.Count; i++)
        {
            await Assert.That(prunedItems[i].Id).IsEqualTo(raindropItems[i].Id);
            await Assert.That(prunedItems[i].Link).IsEqualTo(raindropItems[i].Link);
            await Assert.That(prunedItems[i].Title).IsEqualTo(raindropItems[i].Title);
            await Assert.That(prunedItems[i].Excerpt).IsEqualTo(raindropItems[i].Excerpt);
            await Assert.That(prunedItems[i].Cover).IsEqualTo(raindropItems[i].Cover);
        }
    }

    [Test]
    public async Task ToPruned_NullRaindropItem_ThrowsArgumentNullException()
    {
        // Arrange
        RaindropItem? nullItem = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => nullItem!.ToPruned());
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ToPruned_RaindropItemWithEmptyStrings_PreservesEmptyValues()
    {
        // Arrange
        var raindropItem = new RaindropItem
        {
            Id = 123,
            Link = string.Empty,
            Title = string.Empty,
            Excerpt = string.Empty,
            Cover = string.Empty
        };

        // Act
        var prunedItem = raindropItem.ToPruned();

        // Assert
        await Assert.That(prunedItem).IsNotNull();
        await Assert.That(prunedItem.Id).IsEqualTo(123);
        await Assert.That(prunedItem.Link).IsEqualTo(string.Empty);
        await Assert.That(prunedItem.Title).IsEqualTo(string.Empty);
        await Assert.That(prunedItem.Excerpt).IsEqualTo(string.Empty);
        await Assert.That(prunedItem.Cover).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ToPruned_RaindropItemWithLongContent_PreservesLongContent()
    {
        // Arrange
        var longTitle = new string('A', 400); // 400 characters
        var longExcerpt = new string('B', 1500); // 1500 characters

        var raindropItem = new RaindropItem
        {
            Id = 888,
            Link = "https://example.com/long-content",
            Title = longTitle,
            Excerpt = longExcerpt,
            Cover = "https://example.com/cover.jpg"
        };

        // Act
        var prunedItem = raindropItem.ToPruned();

        // Assert
        await Assert.That(prunedItem.Title).IsEqualTo(longTitle);
        await Assert.That(prunedItem.Excerpt).IsEqualTo(longExcerpt);
        await Assert.That(prunedItem.Title).HasLength().EqualTo(400);
        await Assert.That(prunedItem.Excerpt).HasLength().EqualTo(1500);
    }

    [Test]
    public async Task ToPruned_RaindropItemWithNullValues_HandlesNullsGracefully()
    {
        // Arrange
        var raindropItem = new RaindropItem
        {
            Id = 456,
            Link = null!,
            Title = null!,
            Excerpt = null!,
            Cover = null!
        };

        // Act
        var prunedItem = raindropItem.ToPruned();

        // Assert
        await Assert.That(prunedItem).IsNotNull();
        await Assert.That(prunedItem.Id).IsEqualTo(456);
        await Assert.That(prunedItem.Link).IsNull();
        await Assert.That(prunedItem.Title).IsNull();
        await Assert.That(prunedItem.Excerpt).IsNull();
        await Assert.That(prunedItem.Cover).IsNull();
    }

    [Test]
    public async Task ToPruned_RaindropItemWithSpecialCharacters_PreservesSpecialCharacters()
    {
        // Arrange
        var raindropItem = new RaindropItem
        {
            Id = 999,
            Link = "https://example.com/special?param=value&other=123",
            Title = "Test Title with Special Characters: àáâãäåæçèéêë",
            Excerpt = "Excerpt with emojis 🚀 and symbols: @#$%^&*()_+-=[]{}|;':,.<>?",
            Cover = "https://example.com/cover-with-dashes_and_underscores.jpg"
        };

        // Act
        var prunedItem = raindropItem.ToPruned();

        // Assert
        await Assert.That(prunedItem.Link).IsEqualTo(raindropItem.Link);
        await Assert.That(prunedItem.Title).IsEqualTo(raindropItem.Title);
        await Assert.That(prunedItem.Excerpt).IsEqualTo(raindropItem.Excerpt);
        await Assert.That(prunedItem.Cover).IsEqualTo(raindropItem.Cover);
    }

    [Test]
    public async Task ToPruned_ValidRaindropItem_ReturnsCorrectPrunedItem()
    {
        // Arrange
        var raindropItem = CreateTestRaindropItem();

        // Act
        var prunedItem = raindropItem.ToPruned();

        // Assert
        await Assert.That(prunedItem).IsNotNull();
        await Assert.That(prunedItem.Id).IsEqualTo(raindropItem.Id);
        await Assert.That(prunedItem.Link).IsEqualTo(raindropItem.Link);
        await Assert.That(prunedItem.Title).IsEqualTo(raindropItem.Title);
        await Assert.That(prunedItem.Excerpt).IsEqualTo(raindropItem.Excerpt);
        await Assert.That(prunedItem.Cover).IsEqualTo(raindropItem.Cover);
    }
}