using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TumblThree.Applications.Converter
{
    // this a modified version of this SO-answer: https://stackoverflow.com/a/45505097/14072498
    public class EmptyArrayOrDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.IsAssignableFrom(typeof(Dictionary<string, object>));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            switch (token.Type)
            {
                case JTokenType.Object:
                    return token.ToObject(objectType, serializer);

                case JTokenType.Array:
                    if (!token.HasValues)
                        return Activator.CreateInstance(objectType);
                    else
                        throw new JsonSerializationException("Object or empty array expected");
                default:
                    throw new JsonSerializationException("Object or empty array expected");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, value);
    }
}
