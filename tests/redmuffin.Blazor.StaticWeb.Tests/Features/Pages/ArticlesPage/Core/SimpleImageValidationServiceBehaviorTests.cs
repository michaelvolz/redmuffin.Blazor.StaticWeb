using System.Net;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage.Core;

public sealed class SimpleImageValidationServiceBehaviorTests
{
    [Test]
    public async Task ValidateImageAsync_ValidUrlAndImage_ReturnsSuccess()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();
        var url = "https://example.com/image.jpg";
        infra.SetupResponse(url, HttpStatusCode.OK, "fake-image-bytes");

        var result = await infra.Service.ValidateImageAsync(url).ConfigureAwait(false);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.FailureReason).IsNull();
    }

    [Test]
    public async Task ValidateImageAsync_EmptyUrl_ReturnsFailure()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();

        var result = await infra.Service.ValidateImageAsync("").ConfigureAwait(false);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailureReason).Contains("null or empty");
    }

    [Test]
    public async Task ValidateImageAsync_InvalidUrlFormat_ReturnsFailure()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();

        var result = await infra.Service.ValidateImageAsync("not-a-valid-url").ConfigureAwait(false);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailureReason).Contains("Invalid URL format");
    }

    [Test]
    public async Task ValidateImageAsync_NetworkError_ReturnsFailureWithCaching()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();
        var url = "https://example.com/broken.jpg";
        infra.SetupNetworkError(url, new HttpRequestException("Connection refused"));

        var result = await infra.Service.ValidateImageAsync(url).ConfigureAwait(false);

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task ValidateImageAsync_NonImageContentType_ReturnsFailure()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();
        var url = "https://example.com/data.json";
        infra.SetupResponse(url, HttpStatusCode.OK, "{}", "application/json");

        var result = await infra.Service.ValidateImageAsync(url).ConfigureAwait(false);

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task GetImageUrlOrPlaceholderAsync_ValidImage_ReturnsUrl()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();
        var imageUrl = "https://example.com/valid.jpg";
        infra.SetupResponse(imageUrl, HttpStatusCode.OK, "image-bytes", "image/jpeg");

        var result = await infra.Service.GetImageUrlOrPlaceholderAsync(imageUrl).ConfigureAwait(false);

        await Assert.That(result).IsEqualTo(imageUrl);
    }

    [Test]
    public async Task GetImageUrlOrPlaceholderAsync_InvalidImage_ReturnsPlaceholder()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();
        var imageUrl = "https://example.com/invalid.jpg";
        infra.SetupResponse(imageUrl, HttpStatusCode.OK, "{}", "application/json");

        var result = await infra.Service.GetImageUrlOrPlaceholderAsync(imageUrl).ConfigureAwait(false);

        await Assert.That(result).StartsWith("data:image/svg+xml;base64,");
    }

    [Test]
    public async Task GetCachedResultAsync_CacheHit_ReturnsCachedResult()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();
        var url = "https://example.com/cached.jpg";
        var cached = ImageValidationResult.Success();
        infra.SetupCachedResult(url, cached);

        var result = await infra.Service.GetCachedResultAsync(url).ConfigureAwait(false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsValid).IsTrue();
    }

    [Test]
    public async Task GetCachedResultAsync_CacheMiss_ReturnsNull()
    {
        using var infra = new SimpleImageValidationServiceInfrastructure();
        var url = "https://example.com/not-cached.jpg";

        var result = await infra.Service.GetCachedResultAsync(url).ConfigureAwait(false);

        await Assert.That(result).IsNull();
    }
}
