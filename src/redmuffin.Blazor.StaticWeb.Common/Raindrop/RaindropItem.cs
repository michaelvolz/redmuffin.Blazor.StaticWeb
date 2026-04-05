using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public record RaindropItem
{
    [JsonPropertyName("_id")]
    public long Id { get; init; }

    [JsonPropertyName("link")]
    public string? Link { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("excerpt")]
    public string? Excerpt { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("cover")]
    public string? Cover { get; init; }

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
    public string? Domain { get; init; }

    [JsonPropertyName("collectionId")]
    public long CollectionId { get; init; }

    [JsonPropertyName("sort")]
    public long Sort { get; init; }
}