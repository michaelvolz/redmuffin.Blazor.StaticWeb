using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public record CollectionReference
{
    [JsonPropertyName("$ref")]
    public string? Ref { get; init; }

    [JsonPropertyName("$id")]
    public long? Id { get; init; }

    [JsonPropertyName("oid")]
    public long? Oid { get; init; }
}