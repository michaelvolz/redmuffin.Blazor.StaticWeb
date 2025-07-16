using System.Text.Json;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class CreatorReferenceConverter : JsonConverter<CreatorReference?>
{
    public override CreatorReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
            {
                var id = reader.GetInt64();
                return new CreatorReference { Id = id };
            }

            case JsonTokenType.StartObject:
                return JsonSerializer.Deserialize<CreatorReference>(ref reader, options);

            case JsonTokenType.None:
            case JsonTokenType.EndObject:
            case JsonTokenType.StartArray:
            case JsonTokenType.EndArray:
            case JsonTokenType.PropertyName:
            case JsonTokenType.Comment:
            case JsonTokenType.String:
            case JsonTokenType.True:
            case JsonTokenType.False:
            case JsonTokenType.Null:
            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, CreatorReference? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value != null)
            JsonSerializer.Serialize(writer, value, options);
        else
            writer.WriteNullValue();
    }
}