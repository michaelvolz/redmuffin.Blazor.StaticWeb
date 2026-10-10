using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Api;

public sealed partial class TestDeserialization
{
    /// <summary>
    ///     Validates that the recorded video payload deserializes with the production
    ///     source-generated serializer context and preserves the item data.
    /// </summary>
    [Test]
    public async Task Should_Deserialize_Video_Data_When_Valid_Json_Provided()
    {
        // Arrange
        var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "Videos.json");

        // Act
        var jsonData = await File.ReadAllTextAsync(jsonFilePath).ConfigureAwait(false);
        var videoItems = JsonSerializer.Deserialize<List<RaindropItem>>(
            jsonData,
            RaindropJsonSerializerContext.DefaultOptions
        );

        // Assert
        await Assert.That(videoItems).IsNotNull();
        var items = videoItems!;

        await Assert.That(items.Count).IsGreaterThan(0);

        foreach (var item in items)
            await Assert.That(item.Title).IsNotNull();

        var firstItem = items[0];
        await Assert.That(firstItem.Id).IsEqualTo(1180248514L);
        await Assert
            .That(firstItem.Title)
            .IsEqualTo(
                "The Breakthroughs Needed for AGI Have Already Been Made: OpenAI Former Research Head Bob McGrew"
            );
        await Assert.That(firstItem.Link).IsEqualTo("https://www.youtube.com/watch?v=z_-nLK4Ps1Q");

        await Assert.That(firstItem.Highlights.Count).IsGreaterThan(0);
        await Assert.That(firstItem.Highlights[0].CreatorRef).IsNotNull();
        await Assert.That(firstItem.Highlights[0].CreatorRef!.Id).IsEqualTo(4134988L);
    }
}
