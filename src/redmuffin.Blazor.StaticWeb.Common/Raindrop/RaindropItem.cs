using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public record RaindropItem
{
    [JsonPropertyName("_id")]
    public long Id { get; init; }

    [JsonPropertyName("link")]
    public string Link { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("excerpt")]
    public string Excerpt { get; init; } = string.Empty;

    [JsonPropertyName("note")]
    public string Note { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("user")]
    public UserReference User { get; init; } = new();

    [JsonPropertyName("cover")]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("media")]
    public IReadOnlyList<MediaItem> Media { get; init; } = [];

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = [];

    [JsonPropertyName("important")]
    public bool Important { get; init; }

    [JsonPropertyName("reminder")]
    public Reminder Reminder { get; init; } = new();

    [JsonPropertyName("removed")]
    public bool Removed { get; init; }

    [JsonPropertyName("created")]
    public DateTime Created { get; init; } = DateTime.MinValue;

    [JsonPropertyName("collection")]
    public CollectionReference Collection { get; init; } = new();

    [JsonPropertyName("highlights")]
    public IReadOnlyList<Highlight> Highlights { get; init; } = [];

    [JsonPropertyName("domain")]
    public string Domain { get; init; } = string.Empty;

    [JsonPropertyName("collectionId")]
    public long CollectionId { get; init; }

    [JsonPropertyName("sort")]
    public long Sort { get; init; }
}