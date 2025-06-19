using System.Text.Json;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class Highlight
{
    [JsonPropertyName("text")]
    public string? Text { get; set; } = null;

    [JsonPropertyName("note")]
    public string? Note { get; set; } = null;

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; } = null;

    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; set; } = null;

    [JsonPropertyName("creatorRef")]
    [JsonConverter(typeof(CreatorReferenceConverter))]
    public CreatorReference? CreatorRef { get; set; } = null;
}

public class CreatorReferenceConverter : JsonConverter<CreatorReference?>
{
    public override CreatorReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var id = reader.GetInt64();
            return new CreatorReference { Id = id };
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<CreatorReference>(ref reader, options);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, CreatorReference? value, JsonSerializerOptions options)
    {
        if (value != null)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}