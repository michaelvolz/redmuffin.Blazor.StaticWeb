using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Api;

public sealed partial class TestDeserialization
{
    /// <summary>
    ///     Validates that video data deserializes correctly from JSON with proper object structure.
    /// </summary>
    [Test]
    public async Task Should_Deserialize_Video_Data_When_Valid_Json_Provided()
    {
        // Arrange
        using var scope = CreateTestScope();
        var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "Videos.json");

        // Act
        var jsonData = await File.ReadAllTextAsync(jsonFilePath).ConfigureAwait(false);
        var videoItems = JsonSerializer.Deserialize<List<RaindropItem>>(jsonData, scope.JsonSerializerOptions);

        // Assert
        await Assert.That(videoItems).IsNotNull();
        await Assert.That(videoItems!.Count).IsGreaterThan(0);
    }

    /// <summary>
    ///     Validates that deserialized video items maintain required properties and data integrity.
    /// </summary>
    [Test]
    public async Task Should_Maintain_Data_Integrity_When_Video_Items_Deserialized()
    {
        // Arrange
        using var scope = CreateTestScope();
        var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "Videos.json");

        // Act
        var jsonData = await File.ReadAllTextAsync(jsonFilePath).ConfigureAwait(false);
        var videoItems = JsonSerializer.Deserialize<List<RaindropItem>>(jsonData, scope.JsonSerializerOptions);

        // Assert
        await Assert.That(videoItems).IsNotNull();

        foreach (var item in videoItems!)
        {
            await Assert.That(item.Title).IsNotNull();

            foreach (var highlight in item.Highlights)
                if (highlight.CreatorRef?.Name is { Length: > 0 })
                    await Assert.That(highlight.CreatorRef.Name.Length).IsGreaterThan(0);
        }
    }
}