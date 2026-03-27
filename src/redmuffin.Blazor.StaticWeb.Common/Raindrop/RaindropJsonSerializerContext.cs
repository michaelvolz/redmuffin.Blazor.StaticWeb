using System.Text.Json;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

/// <summary>
///     Enhanced JSON serialization context for RaindropIO API responses with robust error handling
/// </summary>
[JsonSerializable(typeof(List<RaindropItem>), TypeInfoPropertyName = "RaindropItemList")]
[JsonSerializable(typeof(RaindropItem))]
[JsonSerializable(typeof(UserReference))]
[JsonSerializable(typeof(MediaItem))]
[JsonSerializable(typeof(Reminder))]
[JsonSerializable(typeof(CollectionReference))]
[JsonSerializable(typeof(Highlight))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<MediaItem>))]
[JsonSerializable(typeof(List<Highlight>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(DateTime))]
public partial class RaindropJsonSerializerContext : JsonSerializerContext
{
    public static JsonSerializerOptions DefaultOptions { get; } = CreateOptions();

    public static JsonSerializerOptions StrictOptions { get; } = CreateOptions(
        allowTrailingCommas: false,
        commentHandling: JsonCommentHandling.Disallow,
        caseInsensitive: false,
        numberHandling: JsonNumberHandling.Strict,
        ignoreCondition: JsonIgnoreCondition.Never,
        unmappedHandling: JsonUnmappedMemberHandling.Disallow);

    public static JsonSerializerOptions LenientOptions { get; } = CreateOptions(
        numberHandling: JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        indented: true);

    private static JsonSerializerOptions CreateOptions(
        bool allowTrailingCommas = true,
        JsonCommentHandling commentHandling = JsonCommentHandling.Skip,
        bool caseInsensitive = true,
        JsonNumberHandling numberHandling = JsonNumberHandling.AllowReadingFromString,
        JsonIgnoreCondition ignoreCondition = JsonIgnoreCondition.WhenWritingNull,
        JsonUnmappedMemberHandling unmappedHandling = JsonUnmappedMemberHandling.Skip,
        bool indented = false)
    {
        return new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            AllowTrailingCommas = allowTrailingCommas,
            ReadCommentHandling = commentHandling,
            PropertyNameCaseInsensitive = caseInsensitive,
            NumberHandling = numberHandling,
            DefaultIgnoreCondition = ignoreCondition,
            UnmappedMemberHandling = unmappedHandling,
            TypeInfoResolver = Default
        };
    }
}