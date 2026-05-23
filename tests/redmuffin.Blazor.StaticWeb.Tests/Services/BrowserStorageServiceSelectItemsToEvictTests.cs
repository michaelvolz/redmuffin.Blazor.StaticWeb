using BrowserStorageService = redmuffin.Blazor.StaticWeb.Core.Services.BrowserStorageService;

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

public class BrowserStorageServiceSelectItemsToEvictTests
{
    [Test]
    public async Task SelectItemsToEvict_EmptyList_ReturnsNone()
    {
        var result = BrowserStorageService.SelectItemsToEvict([], 100, 50).ToList();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task SelectItemsToEvict_AlreadyUnderTarget_ReturnsNone()
    {
        var items = new List<(string, long, DateTime)>
        {
            ("a", 10, DateTime.UtcNow),
            ("b", 20, DateTime.UtcNow),
        };

        var result = BrowserStorageService.SelectItemsToEvict(items, 30, 50).ToList();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task SelectItemsToEvict_OverTarget_EvictsLRUOrderUntilUnder()
    {
        var older = new DateTime(2020, 1, 1);
        var newer = new DateTime(2024, 1, 1);
        var items = new List<(string, long, DateTime)>
        {
            ("oldest", 40, older),   // first to evict (oldest accessed)
            ("middle", 30, older.AddDays(1)),
            ("newest", 50, newer),  // last to evict
        };

        var result = BrowserStorageService.SelectItemsToEvict(items, 120, 50).ToList();

        // Total=120, target=50. Remove oldest(40)→80, still over. Remove middle(30)→50, at target. Stop.
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Key).IsEqualTo("oldest");
        await Assert.That(result[1].Key).IsEqualTo("middle");
    }

    [Test]
    public async Task SelectItemsToEvict_ExactlyAtTarget_ReturnsNone()
    {
        var items = new List<(string, long, DateTime)>
        {
            ("a", 50, DateTime.UtcNow),
        };

        var result = BrowserStorageService.SelectItemsToEvict(items, 50, 50).ToList();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task SelectItemsToEvict_SingleItemExceedsTarget_EvictsIt()
    {
        var items = new List<(string, long, DateTime)>
        {
            ("big", 100, DateTime.UtcNow),
        };

        var result = BrowserStorageService.SelectItemsToEvict(items, 100, 50).ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Key).IsEqualTo("big");
    }

    [Test]
    public async Task SelectItemsToEvict_AllItemsEvicted_ReturnsAll()
    {
        var items = new List<(string, long, DateTime)>
        {
            ("a", 10, DateTime.UtcNow),
            ("b", 20, DateTime.UtcNow),
            ("c", 30, DateTime.UtcNow),
        };

        var result = BrowserStorageService.SelectItemsToEvict(items, 60, 1).ToList();

        await Assert.That(result.Count).IsEqualTo(3);
    }
}
