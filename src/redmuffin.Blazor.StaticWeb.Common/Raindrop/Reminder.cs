using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class Reminder
{
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; } = null;
}