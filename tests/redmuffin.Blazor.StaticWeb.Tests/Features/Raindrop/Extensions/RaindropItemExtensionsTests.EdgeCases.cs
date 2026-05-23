using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Extensions;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Extensions;

[Category("Feature:RaindropItems")]
public sealed partial class RaindropItemExtensionsTests
{
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
}