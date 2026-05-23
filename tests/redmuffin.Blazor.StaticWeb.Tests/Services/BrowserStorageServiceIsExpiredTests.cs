using redmuffin.Blazor.StaticWeb.Core.Services;
using TUnit;

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

public sealed class BrowserStorageServiceIsExpiredTests
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(7);

    [Test]
    public async Task Should_return_true_when_expires_at_is_in_the_past()
    {
        var now = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        var metadata = new StoredItemMetadata
        {
            CreatedAt = now.AddDays(-1),
            ExpiresAt = now.AddDays(-1)
        };

        var result = BrowserStorageService.IsExpired(metadata, DefaultExpiration, now);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Should_return_false_when_expires_at_is_in_the_future()
    {
        var now = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        var metadata = new StoredItemMetadata
        {
            CreatedAt = now.AddDays(-1),
            ExpiresAt = now.AddDays(1)
        };

        var result = BrowserStorageService.IsExpired(metadata, DefaultExpiration, now);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Should_return_false_when_expires_at_equals_now()
    {
        var now = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        var metadata = new StoredItemMetadata
        {
            CreatedAt = now.AddDays(-1),
            ExpiresAt = now
        };

        var result = BrowserStorageService.IsExpired(metadata, DefaultExpiration, now);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Should_return_true_when_no_expires_at_and_created_before_expiration_window()
    {
        var now = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        var metadata = new StoredItemMetadata
        {
            CreatedAt = now.AddDays(-8),
            ExpiresAt = null
        };

        var result = BrowserStorageService.IsExpired(metadata, DefaultExpiration, now);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Should_return_false_when_no_expires_at_and_created_within_expiration_window()
    {
        var now = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        var metadata = new StoredItemMetadata
        {
            CreatedAt = now.AddDays(-6),
            ExpiresAt = null
        };

        var result = BrowserStorageService.IsExpired(metadata, DefaultExpiration, now);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Should_return_false_when_no_expires_at_and_created_exactly_at_boundary()
    {
        var now = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        var metadata = new StoredItemMetadata
        {
            CreatedAt = now.AddDays(-7),
            ExpiresAt = null
        };

        var result = BrowserStorageService.IsExpired(metadata, DefaultExpiration, now);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Should_use_utc_now_when_utcNow_not_provided()
    {
        var metadata = new StoredItemMetadata
        {
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            ExpiresAt = null
        };

        var result = BrowserStorageService.IsExpired(metadata, DefaultExpiration);

        await Assert.That(result).IsTrue();
    }
}
