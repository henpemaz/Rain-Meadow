using Newtonsoft.Json;
using System;

namespace RainMeadow
{
    public class ShallowJsonDump : JsonConverter
    {
        bool rootDone;
        public override bool CanConvert(Type objectType)
        {
            if (!rootDone)
            {
                rootDone = true;
                return false;
            }
            return true;
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
