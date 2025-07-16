using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Api.Tests;

public class TestDeserialization
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public async Task TestVideosDeserializationAsync()
    {
        try
        {
            var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "Videos.json");
            var jsonData = await File.ReadAllTextAsync(jsonFilePath).ConfigureAwait(false);

            var videoItems = JsonSerializer.Deserialize<List<RaindropItem>>(jsonData, JsonSerializerOptions);

            await Assert.That(videoItems != null).IsTrue();
            await Assert.That(videoItems?.Count > 0).IsTrue();

            foreach (var item in videoItems ?? new List<RaindropItem>())
            {
                await Assert.That(item.Title is { } s).IsTrue();

                foreach (var highlight in item.Highlights)
                    if (highlight.CreatorRef is { Name: { Length: > 0 } })
                        await Assert.That(highlight.CreatorRef?.Name?.Length > 0).IsTrue();
            }
        }
        catch (Exception ex)
        {
            Assert.Fail($"Error during deserialization: {ex.Message}");
        }
    }
}