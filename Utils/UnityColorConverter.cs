using Newtonsoft.Json;
using System;

namespace RainMeadow
{
    public class UnityColorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(UnityEngine.Color).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return RWCustom.Custom.hexToColor((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, RWCustom.Custom.colorToHex((UnityEngine.Color)value));
        }
    }
}
