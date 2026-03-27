using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public record MediaItem
{
    [JsonPropertyName("link")]
    public string? Link { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}