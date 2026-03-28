using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.RaindropItems.Extensions;

/// <summary>
///     Helper methods for RaindropItemExtensionsTests.
/// </summary>
[Category("Feature:RaindropItems")]
public partial class RaindropItemExtensionsTests
{
    /// <summary>
    ///     Creates a test RaindropItem with all essential fields populated.
    /// </summary>
    /// <returns>A configured RaindropItem for testing.</returns>
    private static RaindropItem CreateTestRaindropItem()
    {
        return new RaindropItem
        {
            Id = 12345,
            Link = "https://example.com/test-article",
            Title = "Test Article Title",
            Excerpt = "This is a test excerpt for the article.",
            Cover = "https://example.com/cover.jpg",
            Note = "Test note",
            Type = "article",
            Domain = "example.com",
            Important = true,
            Removed = false,
            Created = DateTime.UtcNow.AddDays(-1),
            CollectionId = 67890,
            Sort = 100
        };
    }

    /// <summary>
    ///     Creates a test PrunedRaindropItem with all fields populated.
    /// </summary>
    /// <returns>A configured PrunedRaindropItem for testing.</returns>
    private static PrunedRaindropItem CreateTestPrunedRaindropItem()
    {
        return new PrunedRaindropItem
        {
            Id = 54321,
            Link = "https://example.com/test-video",
            Title = "Test Video Title",
            Excerpt = "This is a test excerpt for the video.",
            Cover = "https://example.com/video-cover.jpg"
        };
    }

    /// <summary>
    ///     Creates a collection of test RaindropItems for testing collection conversion methods.
    /// </summary>
    /// <returns>A list of configured RaindropItems for testing.</returns>
    private static List<RaindropItem> CreateTestRaindropItemCollection()
    {
        return new List<RaindropItem>
        {
            new()
            {
                Id = 1,
                Link = "https://example.com/item1",
                Title = "First Test Item",
                Excerpt = "First test excerpt",
                Cover = "https://example.com/cover1.jpg",
                Type = "article",
                Domain = "example.com",
                Created = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = 2,
                Link = "https://example.com/item2",
                Title = "Second Test Item",
                Excerpt = "Second test excerpt",
                Cover = "https://example.com/cover2.jpg",
                Type = "video",
                Domain = "example.com",
                Created = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = 3,
                Link = "https://example.com/item3",
                Title = "Third Test Item",
                Excerpt = "Third test excerpt",
                Cover = "https://example.com/cover3.jpg",
                Type = "link",
                Domain = "example.com",
                Created = DateTime.UtcNow.AddDays(-3)
            }
        };
    }

    /// <summary>
    ///     Creates a collection of test PrunedRaindropItems for testing collection conversion methods.
    /// </summary>
    /// <returns>A list of configured PrunedRaindropItems for testing.</returns>
    private static List<PrunedRaindropItem> CreateTestPrunedRaindropItemCollection()
    {
        return new List<PrunedRaindropItem>
        {
            new()
            {
                Id = 101,
                Link = "https://example.com/pruned1",
                Title = "First Pruned Item",
                Excerpt = "First pruned excerpt",
                Cover = "https://example.com/pruned-cover1.jpg"
            },
            new()
            {
                Id = 102,
                Link = "https://example.com/pruned2",
                Title = "Second Pruned Item",
                Excerpt = "Second pruned excerpt",
                Cover = "https://example.com/pruned-cover2.jpg"
            },
            new()
            {
                Id = 103,
                Link = "https://example.com/pruned3",
                Title = "Third Pruned Item",
                Excerpt = "Third pruned excerpt",
                Cover = "https://example.com/pruned-cover3.jpg"
            }
        };
    }

    /// <summary>
    ///     Creates a RaindropItem with minimal data for edge case testing.
    /// </summary>
    /// <returns>A RaindropItem with minimal data.</returns>
    private static RaindropItem CreateMinimalRaindropItem()
    {
        return new RaindropItem
        {
            Id = 999,
            Link = string.Empty,
            Title = string.Empty,
            Excerpt = string.Empty,
            Cover = string.Empty
        };
    }

    /// <summary>
    ///     Creates a PrunedRaindropItem with minimal data for edge case testing.
    /// </summary>
    /// <returns>A PrunedRaindropItem with minimal data.</returns>
    private static PrunedRaindropItem CreateMinimalPrunedRaindropItem()
    {
        return new PrunedRaindropItem
        {
            Id = 888,
            Link = string.Empty,
            Title = string.Empty,
            Excerpt = string.Empty,
            Cover = string.Empty
        };
    }
}