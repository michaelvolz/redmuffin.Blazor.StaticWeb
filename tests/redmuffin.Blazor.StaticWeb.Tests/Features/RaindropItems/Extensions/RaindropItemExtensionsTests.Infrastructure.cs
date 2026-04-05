using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Extensions;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.RaindropItems.Extensions;

[Category("Feature:RaindropItems")]
public sealed partial class RaindropItemExtensionsTests
{
    [Test]
    public async Task ToFull_Collection_ValidItems_ReturnsCorrectRaindropItems()
    {
        // Arrange
        var prunedItems = CreateTestPrunedRaindropItemCollection();

        // Act
        var fullItems = prunedItems.ToFull().ToList();

        // Assert
        await Assert.That(fullItems).Count().IsEqualTo(3);

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
        await Assert.That(fullItem.Note).IsNull();
        await Assert.That(fullItem.Type).IsNull();
        await Assert.That(fullItem.Domain).IsNull();
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
    public async Task ToPruned_Collection_ValidItems_ReturnsCorrectPrunedItems()
    {
        // Arrange
        var raindropItems = CreateTestRaindropItemCollection();

        // Act
        var prunedItems = raindropItems.ToPruned().ToList();

        // Assert
            await Assert.That(prunedItems).Count().IsEqualTo(3);

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