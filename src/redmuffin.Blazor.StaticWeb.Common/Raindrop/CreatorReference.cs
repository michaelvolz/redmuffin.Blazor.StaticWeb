using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class CreatorReference
{
    [JsonPropertyName("_id")]
    public long? Id { get; set; } = null;

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; } = null;

    [JsonPropertyName("name")]
    public string? Name { get; set; } = null;

    [JsonPropertyName("email")]
    public string? Email { get; set; } = null;
}