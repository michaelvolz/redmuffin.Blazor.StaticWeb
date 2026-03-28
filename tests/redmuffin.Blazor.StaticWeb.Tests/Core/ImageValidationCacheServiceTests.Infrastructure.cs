using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed partial class ImageValidationCacheServiceTests
{
    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Cover_When_Cached_As_Valid()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");

        scope.ImageValidationService_Mock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(ImageValidationResult.Success()));

        // Act
        var result = await scope.Service.GetCachedImageUrlAsync(item, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo("https://example.com/cover1.jpg");
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Cover_When_Not_Cached()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");

        scope.ImageValidationService_Mock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(null));

        // Act
        var result = await scope.Service.GetCachedImageUrlAsync(item, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo("https://example.com/cover1.jpg");
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Default_Placeholder_When_No_Cover()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", string.Empty);
        const string expectedPlaceholder = "data:image/svg+xml;base64,placeholder";

        scope.ImagePlaceholderService_Mock
            .Arrange(s => s.GetDefaultPlaceholder())
            .Returns(expectedPlaceholder);

        // Act
        var result = await scope.Service.GetCachedImageUrlAsync(item, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(expectedPlaceholder);
    }

    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Populate_Cache_With_Cached_Results()
    {
        // Arrange
        using var scope = CreateTestScope();
        var items = new[] { CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg") };
        var imageUrlCache = new Dictionary<string, string>();
        var stateChangedCallCount = 0;

        Task StateChangedCallback()
        {
            stateChangedCallCount++;
            return Task.CompletedTask;
        }

        scope.ImageValidationService_Mock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(ImageValidationResult.Success()));

        // Act
        await scope.Service.PopulateImageUrlCacheAsync(items, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache).Count().IsEqualTo(1);
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo("https://example.com/cover1.jpg");
    }

    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Start_Background_Validation_For_Uncached_Items()
    {
        // Arrange
        using var scope = CreateTestScope();
        var items = new[] { CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg") };
        var imageUrlCache = new Dictionary<string, string>();
        var stateChangedCallCount = 0;

        Task StateChangedCallback()
        {
            stateChangedCallCount++;
            return Task.CompletedTask;
        }

        scope.ImageValidationService_Mock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(null));

        scope.ImageValidationService_Mock
            .Arrange(s => s.ValidateImageAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult(ImageValidationResult.Success()));

        // Act
        await scope.Service.PopulateImageUrlCacheAsync(items, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Allow background tasks to complete
        await Task.Delay(100).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache).Count().IsEqualTo(1);
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo("https://example.com/cover1.jpg");
    }

    [Test]
    public async Task ValidateImageInBackgroundAsync_Should_Not_Update_Cache_When_Result_Is_Same()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");
        var imageUrlCache = new Dictionary<string, string> { ["https://example.com/1"] = "https://example.com/cover1.jpg" };
        var stateChangedCallCount = 0;

        Task StateChangedCallback()
        {
            stateChangedCallCount++;
            return Task.CompletedTask;
        }

        scope.ImageValidationService_Mock
            .Arrange(s => s.ValidateImageAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult(ImageValidationResult.Success()));

        // Act
        await scope.Service.ValidateImageInBackgroundAsync(item, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo("https://example.com/cover1.jpg");
        await Assert.That(stateChangedCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task ValidateImageInBackgroundAsync_Should_Update_Cache_When_Validation_Succeeds()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");
        var imageUrlCache = new Dictionary<string, string> { ["https://example.com/1"] = "old_value" };
        var stateChangedCallCount = 0;

        Task StateChangedCallback()
        {
            stateChangedCallCount++;
            return Task.CompletedTask;
        }

        scope.ImageValidationService_Mock
            .Arrange(s => s.ValidateImageAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult(ImageValidationResult.Success()));

        // Act
        await scope.Service.ValidateImageInBackgroundAsync(item, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo("https://example.com/cover1.jpg");
        await Assert.That(stateChangedCallCount).IsEqualTo(1);
    }
}