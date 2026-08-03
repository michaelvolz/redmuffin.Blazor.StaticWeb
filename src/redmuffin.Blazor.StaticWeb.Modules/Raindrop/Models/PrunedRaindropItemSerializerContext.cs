using System.Text.Json;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Models;

[JsonSerializable(typeof(List<PrunedRaindropItem>))]
[JsonSerializable(typeof(PrunedRaindropItem))]
public partial class PrunedRaindropItemSerializerContext : JsonSerializerContext
{
}
