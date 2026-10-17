using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

/// <summary>
///     Trim-safe JSON deserialization for Raindrop data: the single-options entry point
///     plus the shared Default → Lenient → Strict fallback chain used by both RaindropAPI
///     and DummyRaindropAPI.
/// </summary>
public static partial class RaindropJsonDeserializer
{
    /// <summary>
    ///     Deserializes through source-generated metadata, so trimmed publishes get real
    ///     type info from <see cref="JsonSerializerOptions.GetTypeInfo" /> rather than
    ///     reflection.
    /// </summary>
    public static T? Deserialize<T>(string jsonContent, JsonSerializerOptions options)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);

        var typeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        return JsonSerializer.Deserialize(jsonContent, typeInfo);
    }

    /// <summary>
    ///     Tries the Default, Lenient, and Strict option sets in order and returns the
    ///     first non-null result; logs every stage and returns null when all strategies fail.
    /// </summary>
    public static Task<T?> DeserializeWithFallbackAsync<T>(
        string jsonContent,
        ILogger logger,
        string source
    )
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        foreach (
            var (options, strategy) in new (JsonSerializerOptions Options, string Strategy)[]
            {
                (RaindropJsonSerializerContext.DefaultOptions, "DefaultOptions"),
                (RaindropJsonSerializerContext.LenientOptions, "LenientOptions"),
                (RaindropJsonSerializerContext.StrictOptions, "StrictOptions"),
            }
        )
        {
            LogAttemptingDeserialization(logger, source, strategy);
            try
            {
                if (Deserialize<T>(jsonContent, options) is { } result)
                {
                    LogDeserializationSuccess(logger, source, strategy);
                    return Task.FromResult<T?>(result);
                }
            }
            catch (JsonException ex)
            {
                LogDeserializationAttemptFailed(logger, ex, source, strategy);
            }
        }

        LogAllDeserializationStrategiesFailed(logger, source);
        return Task.FromResult<T?>(null);
    }
}
