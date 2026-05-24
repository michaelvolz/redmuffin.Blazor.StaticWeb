using System.Text.Json;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Models;

[JsonSerializable(typeof(List<PrunedRaindropItem>))]
[JsonSerializable(typeof(PrunedRaindropItem))]
public partial class PrunedRaindropItemSerializerContext : JsonSerializerContext
{
}
