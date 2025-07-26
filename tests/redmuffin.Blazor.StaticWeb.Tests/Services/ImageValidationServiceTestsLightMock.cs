using LightMock;
using LightMock.Generator;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Services;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

/// <summary>
///     Tests using LightMock.Generator for comparison with NSubstitute.
///     Following the Mock suffix naming conventions.
/// </summary>
public class ImageValidationServiceTestsLightMock : IDisposable
{
    public ImageValidationServiceTestsLightMock()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ImageValidationService>>();

        _service = new ImageValidationService(
            _httpClientFactoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object
        );
    }

    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ImageValidationService>> _loggerMock;
    private readonly ImageValidationService _service;

    /// <summary>
    ///     Tests ClearValidationCacheAsync clears memory and persistent cache using LightMock.Generator.
    /// </summary>
    [Test]
    [Skip("Needs Migration")]
    public async Task ClearValidationCacheAsync_ClearsBothCaches_LightMock()
    {
        // Act
        await _service.ClearValidationCacheAsync().ConfigureAwait(false);

        // Assert
        _cacheServiceMock.Assert(f => f.ClearNamespaceAsync("image_validation", The<CancellationToken>.IsAnyValue));
        // Hard to test _memoryCache, ensure no exceptions
    }
}