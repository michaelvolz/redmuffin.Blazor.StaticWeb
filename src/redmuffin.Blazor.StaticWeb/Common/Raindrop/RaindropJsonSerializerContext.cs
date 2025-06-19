using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

[JsonSerializable(typeof(List<RaindropItem>))]
public partial class RaindropJsonSerializerContext : JsonSerializerContext
{
}