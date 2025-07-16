using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class UserReference
{
    [JsonPropertyName("$ref")]
    public string? Ref { get; set; } = null;

    [JsonPropertyName("$id")]
    public long? Id { get; set; } = null;
}