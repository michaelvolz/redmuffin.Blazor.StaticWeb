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
    /// <summary>
    ///     Default JSON serialization options with enhanced error handling for malformed responses
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        TypeInfoResolver = Default
    };

    /// <summary>
    ///     Strict JSON serialization options for production API calls with minimal tolerance
    /// </summary>
    public static JsonSerializerOptions StrictOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        PropertyNameCaseInsensitive = false,
        NumberHandling = JsonNumberHandling.Strict,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        TypeInfoResolver = Default
    };

    /// <summary>
    ///     Lenient JSON serialization options for dummy data and development scenarios
    /// </summary>
    public static JsonSerializerOptions LenientOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        TypeInfoResolver = Default
    };
}