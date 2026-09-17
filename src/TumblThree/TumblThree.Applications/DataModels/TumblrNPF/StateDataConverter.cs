using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    public class StateDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true; // Applied directly via attribute, so true is fine
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            // The JSON is an array -> Return the list directly!
            if (token.Type == JTokenType.Array)
            {
                var list = new List<Datum>();
                var array = (JArray)token;

                foreach (var item in array)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        list.Add(item.ToObject<Datum>(serializer));
                    }
                }
                return list;
            }

            // The JSON is a single object
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj["isLoggedIn"] != null)
                {
                    return obj.ToObject<User>(serializer);
                }

                if (obj["isMember"] != null)
                {
                    return obj.ToObject<Community>(serializer);
                }

                if (obj["pages"] != null)
                {
                    return obj.ToObject<DataPages>(serializer);
                }

                // If it's an empty object {}, return null
                if (!obj.HasValues)
                {
                    return null;
                }
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Serialization handles single objects and lists natively out of the box
            serializer.Serialize(writer, value);
        }
    }
}
