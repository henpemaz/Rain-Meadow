using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        public static NoCustomPropertiesResolver customResolver = new();
        public class NoCustomPropertiesResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
                var propsToIgnore = type.GetProperties().Where(p => !IsAutoProperty(p)).Select(p => p.Name).ToList();
                properties = properties.Where(p => !propsToIgnore.Contains(p.PropertyName)).ToList();

                return properties;
            }

            public static bool IsAutoProperty(PropertyInfo prop)
            {
                return prop.CanWrite && prop.CanRead && prop.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Any(f => f.Name.Contains("<" + prop.Name + ">"));
            }
        }
    }
}
