using System.Text.Json;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

/// <summary>
///     Enhanced JSON serialization context for RaindropIO API responses with robust error handling
/// </summary>
[JsonSerializable(typeof(List<RaindropItem>), TypeInfoPropertyName = "RaindropItemList")]
[JsonSerializable(typeof(RaindropItem))]
[JsonSerializable(typeof(RaindropItem[]))]
[JsonSerializable(typeof(UserReference))]
[JsonSerializable(typeof(CreatorReference))]
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
    // Lazy: user field initializers run before the generated Default field is assigned,
    // so eager creation once captured a null resolver.
    private static readonly Lazy<JsonSerializerOptions> LazyDefaultOptions = new(() =>
        CreateOptions()
    );

    private static readonly Lazy<JsonSerializerOptions> LazyStrictOptions = new(() =>
        CreateOptions(
            allowTrailingCommas: false,
            commentHandling: JsonCommentHandling.Disallow,
            caseInsensitive: false,
            numberHandling: JsonNumberHandling.Strict,
            ignoreCondition: JsonIgnoreCondition.Never,
            unmappedHandling: JsonUnmappedMemberHandling.Disallow
        )
    );

    private static readonly Lazy<JsonSerializerOptions> LazyLenientOptions = new(() =>
        CreateOptions(
            numberHandling: JsonNumberHandling.AllowReadingFromString
                | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            indented: true
        )
    );

    public static JsonSerializerOptions DefaultOptions => LazyDefaultOptions.Value;

    public static JsonSerializerOptions StrictOptions => LazyStrictOptions.Value;

    public static JsonSerializerOptions LenientOptions => LazyLenientOptions.Value;

    private static JsonSerializerOptions CreateOptions(
        bool allowTrailingCommas = true,
        JsonCommentHandling commentHandling = JsonCommentHandling.Skip,
        bool caseInsensitive = true,
        JsonNumberHandling numberHandling = JsonNumberHandling.AllowReadingFromString,
        JsonIgnoreCondition ignoreCondition = JsonIgnoreCondition.WhenWritingNull,
        JsonUnmappedMemberHandling unmappedHandling = JsonUnmappedMemberHandling.Skip,
        bool indented = false
    )
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
            TypeInfoResolver = Default,
        };
    }
}
