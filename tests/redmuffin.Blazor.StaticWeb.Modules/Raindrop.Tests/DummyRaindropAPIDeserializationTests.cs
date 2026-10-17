using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Tests;

public class DummyRaindropAPIDeserializationTests
{
    private static readonly ILogger Logger = NullLogger.Instance;

    [Test]
    public async Task DeserializeWithFallbackAsync_ValidJson_ReturnsResult()
    {
        var json = """[{"title":"Test Item","link":"https://example.com"}]""";

        var result = await RaindropJsonDeserializer
            .DeserializeWithFallbackAsync<List<RaindropItem>>(json, Logger, "test.json")
            .ConfigureAwait(false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Test Item");
    }

    [Test]
    public async Task DeserializeWithFallbackAsync_EmptyString_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            RaindropJsonDeserializer.DeserializeWithFallbackAsync<List<RaindropItem>>(
                "",
                Logger,
                "test.json"
            )
        );
    }

    [Test]
    public async Task DeserializeWithFallbackAsync_NullFileName_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            RaindropJsonDeserializer.DeserializeWithFallbackAsync<List<RaindropItem>>(
                "[]",
                Logger,
                " "
            )
        );
    }

    [Test]
    public async Task DeserializeWithFallbackAsync_CompletelyInvalidJson_ReturnsNull()
    {
        var json = "NOT JSON AT ALL {{{";

        var result = await RaindropJsonDeserializer
            .DeserializeWithFallbackAsync<List<RaindropItem>>(json, Logger, "test.json")
            .ConfigureAwait(false);

        await Assert.That(result).IsNull();
    }
}
