using System.Text.Json.Serialization;
using JetBrains.Annotations;

#pragma warning disable MA0048 //Disable warning for file name not matching type name

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

[UsedImplicitly]
public class RaindropItem
{
	[JsonPropertyName("_id")]
	public long Id { get; set; }

	[JsonPropertyName("link")]
	public string Link { get; set; } = string.Empty;

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("excerpt")]
	public string Excerpt { get; set; } = string.Empty;

	[JsonPropertyName("note")]
	public string Note { get; set; } = string.Empty;

	[JsonPropertyName("type")]
	public string Type { get; set; } = string.Empty;

	[JsonPropertyName("user")]
	public UserReference User { get; set; } = new();

	[JsonPropertyName("cover")]
	public string Cover { get; set; } = string.Empty;

	[JsonPropertyName("media")]
	public IList<MediaItem> Media { get; set; } = new List<MediaItem>();

	[JsonPropertyName("tags")]
	public IList<string> Tags { get; set; } = new List<string>();

	[JsonPropertyName("important")]
	public bool Important { get; set; }

	[JsonPropertyName("reminder")]
	public Reminder Reminder { get; set; } = new();

	[JsonPropertyName("removed")]
	public bool Removed { get; set; }

	[JsonPropertyName("created")]
	public DateTime Created { get; set; } = DateTime.MinValue;

	[JsonPropertyName("collection")]
	public CollectionReference Collection { get; set; } = new();

	[JsonPropertyName("highlights")]
	public IList<Highlight> Highlights { get; set; } = new List<Highlight>();

	[JsonPropertyName("domain")]
	public string Domain { get; set; } = string.Empty;

	[JsonPropertyName("collectionId")]
	public long CollectionId { get; set; }

	[JsonPropertyName("sort")]
	public long Sort { get; set; }
}