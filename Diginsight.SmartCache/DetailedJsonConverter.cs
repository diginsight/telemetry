using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diginsight.SmartCache;

internal sealed class DetailedJsonConverter : JsonConverter
{
    public override bool CanRead => false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        JToken tempJt;
        using (JTokenWriter tempWriter = new ())
        {
            serializer.Serialize(tempWriter, value, typeof(object));
            tempJt = tempWriter.Token!;
        }

        Type objectType = value.GetType();
        if ((tempJt is JObject && tempJt["$type"] is not null)
            ||
            (tempJt is JValue && JsonConvert.DeserializeObject<JValue>(tempJt.ToString(Formatting.None))!.Value!.GetType() == objectType))
        {
            tempJt.WriteTo(writer);
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName("$type", false);
        serializer.Serialize(writer, objectType);
        writer.WritePropertyName("$value", false);
        tempJt.WriteTo(writer);

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }

    public override bool CanConvert(Type objectType)
    {
        throw new NotSupportedException();
    }
}
