using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Models;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;
using TUnit.Assertions;
using TUnit.Core;
using CoreImageValidationResult = redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models.ImageValidationResult;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Core;

/// <summary>
/// TUnit tests for ImageValidationCacheService.
/// </summary>
public sealed partial class ImageValidationCacheServiceTests
{
    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Populate_Cache_With_Cached_Results()
    {
        // Arrange
        using var scope = CreateTestScope();
        var items = new[] { CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg") };
        var imageUrlCache = new Dictionary<string, string>();
        var stateChangedCallCount = 0;
        Task StateChangedCallback() { stateChangedCallCount++; return Task.CompletedTask; }

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(ImageValidationResult.Success()));

        // Act
        await scope.Service.PopulateImageUrlCacheAsync(items, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache).HasCount().EqualTo(1);
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
        Task StateChangedCallback() { stateChangedCallCount++; return Task.CompletedTask; }

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(null));

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.ValidateImageAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult(ImageValidationResult.Success()));

        // Act
        await scope.Service.PopulateImageUrlCacheAsync(items, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Allow background tasks to complete
        await Task.Delay(100).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache).HasCount().EqualTo(1);
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo("https://example.com/cover1.jpg");
    }

    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Handle_Empty_Items_Collection()
    {
        // Arrange
        using var scope = CreateTestScope();
        var items = Array.Empty<RaindropItem>();
        var imageUrlCache = new Dictionary<string, string>();
        var stateChangedCallCount = 0;
        Task StateChangedCallback() { stateChangedCallCount++; return Task.CompletedTask; }

        // Act
        await scope.Service.PopulateImageUrlCacheAsync(items, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache).IsEmpty();
        await Assert.That(stateChangedCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Default_Placeholder_When_No_Cover()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", string.Empty);
        const string expectedPlaceholder = "data:image/svg+xml;base64,placeholder";

        scope.ImagePlaceholderServiceMock
            .Arrange(s => s.GetDefaultPlaceholder())
            .Returns(expectedPlaceholder);

        // Act
        var result = await scope.Service.GetCachedImageUrlAsync(item, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(expectedPlaceholder);
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Cover_When_Cached_As_Valid()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(ImageValidationResult.Success()));

        // Act
        var result = await scope.Service.GetCachedImageUrlAsync(item, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo("https://example.com/cover1.jpg");
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Placeholder_When_Cached_As_Invalid()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");
        const string expectedPlaceholder = "data:image/svg+xml;base64,placeholder";
        const string failureReason = "Image not found";

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(ImageValidationResult.Failure(failureReason)));

        scope.ImagePlaceholderServiceMock
            .Arrange(s => s.GenerateSimplePlaceholder(failureReason))
            .Returns(expectedPlaceholder);

        // Act
        var result = await scope.Service.GetCachedImageUrlAsync(item, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(expectedPlaceholder);
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Cover_When_Not_Cached()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.GetCachedResultAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(null));

        // Act
        var result = await scope.Service.GetCachedImageUrlAsync(item, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo("https://example.com/cover1.jpg");
    }

    [Test]
    public async Task ValidateImageInBackgroundAsync_Should_Update_Cache_When_Validation_Succeeds()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");
        var imageUrlCache = new Dictionary<string, string> { ["https://example.com/1"] = "old_value" };
        var stateChangedCallCount = 0;
        Task StateChangedCallback() { stateChangedCallCount++; return Task.CompletedTask; }

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.ValidateImageAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult(ImageValidationResult.Success()));

        // Act
        await scope.Service.ValidateImageInBackgroundAsync(item, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo("https://example.com/cover1.jpg");
        await Assert.That(stateChangedCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task ValidateImageInBackgroundAsync_Should_Update_Cache_With_Placeholder_When_Validation_Fails()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");
        var imageUrlCache = new Dictionary<string, string> { ["https://example.com/1"] = "old_value" };
        var stateChangedCallCount = 0;
        Task StateChangedCallback() { stateChangedCallCount++; return Task.CompletedTask; }
        const string expectedPlaceholder = "data:image/svg+xml;base64,placeholder";
        const string failureReason = "Image not found";

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.ValidateImageAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult(ImageValidationResult.Failure(failureReason)));

        scope.ImagePlaceholderServiceMock
            .Arrange(s => s.GenerateSimplePlaceholder(failureReason))
            .Returns(expectedPlaceholder);

        // Act
        await scope.Service.ValidateImageInBackgroundAsync(item, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo(expectedPlaceholder);
        await Assert.That(stateChangedCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task ValidateImageInBackgroundAsync_Should_Not_Update_Cache_When_Result_Is_Same()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");
        var imageUrlCache = new Dictionary<string, string> { ["https://example.com/1"] = "https://example.com/cover1.jpg" };
        var stateChangedCallCount = 0;
        Task StateChangedCallback() { stateChangedCallCount++; return Task.CompletedTask; }

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.ValidateImageAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Returns(Task.FromResult(ImageValidationResult.Success()));

        // Act
        await scope.Service.ValidateImageInBackgroundAsync(item, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo("https://example.com/cover1.jpg");
        await Assert.That(stateChangedCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task ValidateImageInBackgroundAsync_Should_Handle_Empty_Cover_Gracefully()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", string.Empty);
        var imageUrlCache = new Dictionary<string, string>();
        var stateChangedCallCount = 0;
        Task StateChangedCallback() { stateChangedCallCount++; return Task.CompletedTask; }

        // Act
        await scope.Service.ValidateImageInBackgroundAsync(item, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache).IsEmpty();
        await Assert.That(stateChangedCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task ValidateImageInBackgroundAsync_Should_Handle_Validation_Exception_Gracefully()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem("https://example.com/1", "https://example.com/cover1.jpg");
        var imageUrlCache = new Dictionary<string, string> { ["https://example.com/1"] = "old_value" };
        var stateChangedCallCount = 0;
        Task StateChangedCallback() { stateChangedCallCount++; return Task.CompletedTask; }
        const string expectedPlaceholder = "data:image/svg+xml;base64,placeholder";

        scope.SimpleImageValidationServiceMock
            .Arrange(s => s.ValidateImageAsync("https://example.com/cover1.jpg", CancellationToken.None))
            .Throws<InvalidOperationException>();

        scope.ImagePlaceholderServiceMock
            .Arrange(s => s.GenerateSimplePlaceholder("Validation error"))
            .Returns(expectedPlaceholder);

        // Act
        await scope.Service.ValidateImageInBackgroundAsync(item, imageUrlCache, StateChangedCallback, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache["https://example.com/1"]).IsEqualTo(expectedPlaceholder);
        await Assert.That(stateChangedCallCount).IsEqualTo(1);
    }

}