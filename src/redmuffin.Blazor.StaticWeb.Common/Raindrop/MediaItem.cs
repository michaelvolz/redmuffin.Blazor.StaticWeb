using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class MediaItem
{
	[JsonPropertyName("link")]
	public string? Link { get; set; } = null;

	[JsonPropertyName("type")]
	public string? Type { get; set; } = null;
}