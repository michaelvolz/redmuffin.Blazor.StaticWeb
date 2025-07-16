using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class Highlight
{
    [JsonPropertyName("text")]
    public string? Text { get; set; } = null;

    [JsonPropertyName("note")]
    public string? Note { get; set; } = null;

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; } = null;

    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; set; } = null;

    [JsonPropertyName("creatorRef")]
    [JsonConverter(typeof(CreatorReferenceConverter))]
    public CreatorReference? CreatorRef { get; set; } = null;
}