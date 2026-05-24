using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Extensions;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Extensions;

/// <summary>
///     Unit tests for RaindropItemExtensions conversion methods.
///     Tests the conversion between RaindropItem and PrunedRaindropItem models.
/// </summary>
[Category("Feature:RaindropItems")]
[Category("Unit")]
public partial class RaindropItemExtensionsTests
{
    [Test]
    public async Task RoundTrip_Collection_ToPrunedAndToUnpruned_PreservesEssentialData()
    {
        // Arrange
        var originalItems = CreateTestRaindropItemCollection();

        // Act
        var prunedItems = originalItems.ToPruned().ToList();
        var roundTripItems = prunedItems.ToUnpruned().ToList();

        // Assert
        await Assert.That(roundTripItems).Count().IsEqualTo(originalItems.Count);

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
    public async Task RoundTrip_ToPrunedAndToUnpruned_PreservesEssentialData()
    {
        // Arrange
        var originalItem = CreateTestRaindropItem();

        // Act
        var prunedItem = originalItem.ToPruned();
        var roundTripItem = prunedItem.ToUnpruned();

        // Assert
        await Assert.That(roundTripItem.Id).IsEqualTo(originalItem.Id);
        await Assert.That(roundTripItem.Link).IsEqualTo(originalItem.Link);
        await Assert.That(roundTripItem.Title).IsEqualTo(originalItem.Title);
        await Assert.That(roundTripItem.Excerpt).IsEqualTo(originalItem.Excerpt);
        await Assert.That(roundTripItem.Cover).IsEqualTo(originalItem.Cover);
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
        await Assert.That(prunedItem.Title).Length().IsEqualTo(400);
        await Assert.That(prunedItem.Excerpt).Length().IsEqualTo(1500);
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
}