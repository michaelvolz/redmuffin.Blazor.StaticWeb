using System.Text.Json;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

[JsonSerializable(typeof(List<RaindropItem>), TypeInfoPropertyName = "RaindropItemList")]
[JsonSerializable(typeof(UserReference))]
[JsonSerializable(typeof(MediaItem))]
[JsonSerializable(typeof(Reminder))]
[JsonSerializable(typeof(CollectionReference))]
[JsonSerializable(typeof(Highlight))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<MediaItem>))]
[JsonSerializable(typeof(List<Highlight>))]
public partial class RaindropJsonSerializerContext : JsonSerializerContext
{
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}