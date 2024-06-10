using Newtonsoft.Json;
using System;

namespace RainMeadow
{
    public class WorldCoordinateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(WorldCoordinate).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return WorldCoordinate.FromString((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((WorldCoordinate)value).SaveToString());
        }
    }
}
