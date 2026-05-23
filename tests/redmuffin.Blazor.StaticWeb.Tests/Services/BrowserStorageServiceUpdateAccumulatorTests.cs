namespace redmuffin.Blazor.StaticWeb.Tests.Services;

using redmuffin.Blazor.StaticWeb.Core.Services;
using TUnit;

public sealed class BrowserStorageServiceUpdateAccumulatorTests
{
    private static readonly DateTime BaseTime = new(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Should_add_size_when_key_not_in_index()
    {
        var acc = new BrowserStorageService.StatsAccumulator();
        var index = new Dictionary<string, StoredItemMetadata>();

        BrowserStorageService.UpdateAccumulator(acc, "key1", 100, index);

        await Assert.That(acc.TotalSize).IsEqualTo(100);
        await Assert.That(acc.ExpiredCount).IsEqualTo(0);
        await Assert.That(acc.Oldest).IsNull();
        await Assert.That(acc.Newest).IsNull();
    }

    [Test]
    public async Task Should_add_size_and_set_timestamps_when_item_not_expired()
    {
        var acc = new BrowserStorageService.StatsAccumulator();
        var index = new Dictionary<string, StoredItemMetadata>
        {
            ["key1"] = new()
            {
                CreatedAt = BaseTime,
                ExpiresAt = BaseTime.AddDays(30)
            }
        };

        BrowserStorageService.UpdateAccumulator(acc, "key1", 200, index);

        await Assert.That(acc.TotalSize).IsEqualTo(200);
        await Assert.That(acc.ExpiredCount).IsEqualTo(0);
        await Assert.That(acc.Oldest).IsEqualTo(BaseTime);
        await Assert.That(acc.Newest).IsEqualTo(BaseTime);
    }

    [Test]
    public async Task Should_count_expired_when_item_is_expired()
    {
        var acc = new BrowserStorageService.StatsAccumulator();
        var index = new Dictionary<string, StoredItemMetadata>
        {
            ["key1"] = new()
            {
                CreatedAt = BaseTime.AddDays(-8),
                ExpiresAt = null
            }
        };

        BrowserStorageService.UpdateAccumulator(acc, "key1", 300, index);

        await Assert.That(acc.ExpiredCount).IsEqualTo(1);
        await Assert.That(acc.TotalSize).IsEqualTo(300);
    }

    [Test]
    public async Task Should_update_oldest_when_new_item_is_older()
    {
        var acc = new BrowserStorageService.StatsAccumulator
        {
            Oldest = BaseTime,
            Newest = BaseTime
        };
        var index = new Dictionary<string, StoredItemMetadata>
        {
            ["key2"] = new()
            {
                CreatedAt = BaseTime.AddDays(-1),
                ExpiresAt = BaseTime.AddDays(30)
            }
        };

        BrowserStorageService.UpdateAccumulator(acc, "key2", 100, index);

        await Assert.That(acc.Oldest).IsEqualTo(BaseTime.AddDays(-1));
    }

    [Test]
    public async Task Should_update_newest_when_new_item_is_newer()
    {
        var acc = new BrowserStorageService.StatsAccumulator
        {
            Oldest = BaseTime,
            Newest = BaseTime
        };
        var index = new Dictionary<string, StoredItemMetadata>
        {
            ["key2"] = new()
            {
                CreatedAt = BaseTime.AddDays(1),
                ExpiresAt = BaseTime.AddDays(30)
            }
        };

        BrowserStorageService.UpdateAccumulator(acc, "key2", 100, index);

        await Assert.That(acc.Newest).IsEqualTo(BaseTime.AddDays(1));
    }

    [Test]
    public async Task Should_accumulate_across_multiple_calls()
    {
        var acc = new BrowserStorageService.StatsAccumulator();
        var index = new Dictionary<string, StoredItemMetadata>
        {
            ["key1"] = new()
            {
                CreatedAt = BaseTime.AddDays(-2),
                ExpiresAt = BaseTime.AddDays(30)
            },
            ["key2"] = new()
            {
                CreatedAt = BaseTime,
                ExpiresAt = BaseTime.AddDays(30)
            },
            ["key3"] = new()
            {
                CreatedAt = BaseTime.AddDays(-8),
                ExpiresAt = null // expired (default 7-day)
            }
        };

        BrowserStorageService.UpdateAccumulator(acc, "key1", 100, index);
        BrowserStorageService.UpdateAccumulator(acc, "key2", 200, index);
        BrowserStorageService.UpdateAccumulator(acc, "key3", 50, index);

        await Assert.That(acc.TotalSize).IsEqualTo(350);
        await Assert.That(acc.ExpiredCount).IsEqualTo(1);
        await Assert.That(acc.Oldest).IsEqualTo(BaseTime.AddDays(-8));
        await Assert.That(acc.Newest).IsEqualTo(BaseTime);
    }

    [Test]
    public async Task Should_not_change_oldest_when_item_is_not_older()
    {
        var acc = new BrowserStorageService.StatsAccumulator
        {
            Oldest = BaseTime.AddDays(-5),
            Newest = BaseTime
        };
        var index = new Dictionary<string, StoredItemMetadata>
        {
            ["key"] = new()
            {
                CreatedAt = BaseTime.AddDays(-1),
                ExpiresAt = BaseTime.AddDays(30)
            }
        };

        BrowserStorageService.UpdateAccumulator(acc, "key", 100, index);

        await Assert.That(acc.Oldest).IsEqualTo(BaseTime.AddDays(-5));
    }

    [Test]
    public async Task Should_not_change_newest_when_item_is_not_newer()
    {
        var acc = new BrowserStorageService.StatsAccumulator
        {
            Oldest = BaseTime.AddDays(-5),
            Newest = BaseTime
        };
        var index = new Dictionary<string, StoredItemMetadata>
        {
            ["key"] = new()
            {
                CreatedAt = BaseTime.AddDays(-1),
                ExpiresAt = BaseTime.AddDays(30)
            }
        };

        BrowserStorageService.UpdateAccumulator(acc, "key", 100, index);

        await Assert.That(acc.Newest).IsEqualTo(BaseTime);
    }
}
