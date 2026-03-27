using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public record Highlight
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; init; }

    [JsonPropertyName("creatorRef")]
    [JsonConverter(typeof(CreatorReferenceConverter))]
    public CreatorReference? CreatorRef { get; init; }
}