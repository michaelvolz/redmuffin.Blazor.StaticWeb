using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public record Reminder
{
    [JsonPropertyName("date")]
    public DateTime? Date { get; init; }
}