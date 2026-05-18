using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DummyRaindropAPI = redmuffin.Blazor.StaticWeb.Features.Raindrop.Services.DummyRaindropAPI;
using RaindropItem = redmuffin.Blazor.StaticWeb.Common.Raindrop.RaindropItem;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Services;

public class DummyRaindropAPIDeserializationTests
{
    private static readonly ILogger _logger = NullLogger.Instance;

    [Test]
    public async Task DeserializeWithFallbackAsync_ValidJson_ReturnsResult()
    {
        var json = """[{"title":"Test Item","link":"https://example.com"}]""";

        var result = await DummyRaindropAPI.DeserializeWithFallbackAsync<List<RaindropItem>>(
            json, "test.json", _logger, CancellationToken.None).ConfigureAwait(false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Test Item");
    }

    [Test]
    public async Task DeserializeWithFallbackAsync_EmptyString_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            DummyRaindropAPI.DeserializeWithFallbackAsync<List<RaindropItem>>(
                "", "test.json", _logger, CancellationToken.None));
    }

    [Test]
    public async Task DeserializeWithFallbackAsync_NullFileName_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            DummyRaindropAPI.DeserializeWithFallbackAsync<List<RaindropItem>>(
                "[]", " ", _logger, CancellationToken.None));
    }

    [Test]
    public async Task DeserializeWithFallbackAsync_CompletelyInvalidJson_ReturnsNull()
    {
        var json = "NOT JSON AT ALL {{{";

        var result = await DummyRaindropAPI.DeserializeWithFallbackAsync<List<RaindropItem>>(
            json, "test.json", _logger, CancellationToken.None).ConfigureAwait(false);

        await Assert.That(result).IsNull();
    }
}
