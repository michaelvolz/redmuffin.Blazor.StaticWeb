using System.Text.Json.Serialization;
using JetBrains.Annotations;

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
    public IList<MediaItem> Media { get; } = new List<MediaItem>();

    [JsonPropertyName("tags")]
    public IList<string> Tags { get; } = new List<string>();

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
    public IList<Highlight> Highlights { get; } = new List<Highlight>();

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("collectionId")]
    public long CollectionId { get; set; }

    [JsonPropertyName("sort")]
    public long Sort { get; set; }
}