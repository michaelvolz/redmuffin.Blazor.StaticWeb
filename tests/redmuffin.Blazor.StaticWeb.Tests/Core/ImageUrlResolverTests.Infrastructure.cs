namespace redmuffin.Blazor.StaticWeb.Tests.Core;

[Category("Feature:Core")]
public sealed partial class ImageUrlResolverTests
{
    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Use_Cover_Without_Validation_When_Cache_Empty()
    {
        // Arrange
        using var scope = CreateTestScope();
        var items = new[]
        {
            CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg"),
        };
        var imageUrlCache = new Dictionary<string, string>();
        var stateChangedCallCount = 0;

        Task StateChangedCallback()
        {
            stateChangedCallCount++;
            return Task.CompletedTask;
        }

        // Act
        await scope
            .Service.PopulateImageUrlCacheAsync(
                items,
                imageUrlCache,
                StateChangedCallback,
                CancellationToken.None
            )
            .ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache).Count().IsEqualTo(1);
        await Assert
            .That(imageUrlCache["https://example.com/1"])
            .IsEqualTo("https://example.com/cover1.jpg");
        await Assert.That(stateChangedCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Use_Default_Placeholder_When_Cover_Missing()
    {
        // Arrange
        using var scope = CreateTestScope();
        var items = new[] { CreateTestItem("https://example.com/1", string.Empty) };
        const string expectedPlaceholder = "data:image/svg+xml;base64,placeholder";
        var imageUrlCache = new Dictionary<string, string>();

        scope
            .ImagePlaceholderService_Mock.Arrange(s => s.GetDefaultPlaceholder())
            .Returns(expectedPlaceholder);

        // Act
        await scope
            .Service.PopulateImageUrlCacheAsync(
                items,
                imageUrlCache,
                () => Task.CompletedTask,
                CancellationToken.None
            )
            .ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo(expectedPlaceholder);
    }
}
