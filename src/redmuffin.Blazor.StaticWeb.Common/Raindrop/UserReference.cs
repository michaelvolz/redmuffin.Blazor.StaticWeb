using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public record UserReference
{
    [JsonPropertyName("$ref")]
    public string? Ref { get; init; }

    [JsonPropertyName("$id")]
    public long? Id { get; init; }
}