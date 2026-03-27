using System.Text.Json;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class CreatorReferenceConverter : JsonConverter<CreatorReference?>
{
    public override CreatorReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => new CreatorReference { Id = reader.GetInt64() },
            JsonTokenType.StartObject => JsonSerializer.Deserialize<CreatorReference>(ref reader, options),
            _ => null
        };
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