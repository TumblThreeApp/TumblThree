using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TumblThree.Applications.Converter
{
    /// <summary>
    /// Flexible naming (camel and snake case) converter (includes SingleOrArrayConverter)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FlexibleNamingConverter<T> : JsonConverter
    {
        //public override bool CanConvert(Type objectType) => objectType == typeof(T);
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T) || objectType == typeof(List<T>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (objectType == typeof(List<T>))
            {
                var result = new List<T>();
                if (token.Type == JTokenType.Array)
                {
                    foreach (var item in token)
                    {
                        result.Add(ParseFlexibleObject(item, serializer));
                    }
                }
                else
                {
                    result.Add(ParseFlexibleObject(token, serializer));
                }
                return result;
            }

            return ParseFlexibleObject(token, serializer);
        }

        private static T ParseFlexibleObject(JToken token, JsonSerializer serializer)
        {
            JObject jo = token as JObject ?? JObject.FromObject(token);
            var obj = Activator.CreateInstance<T>();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var jsonKeys = new[] {
                prop.Name,
                ToCamelCase(prop.Name),
                ToSnakeCase(prop.Name)
            };

                foreach (var key in jsonKeys)
                {
                    var property = jo.Property(key, StringComparison.OrdinalIgnoreCase);
                    if (property != null)
                    {
                        var value = property.Value.ToObject(prop.PropertyType, serializer);
                        prop.SetValue(obj, value);
                        break;
                    }
                }
            }

            return obj;
        }

        /*
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            var obj = Activator.CreateInstance<T>();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var jsonKeys = new[] {
                    prop.Name,
                    ToCamelCase(prop.Name),
                    ToSnakeCase(prop.Name)
                };

                foreach (var key in jsonKeys)
                {
                    var token = jo.Property(key, StringComparison.OrdinalIgnoreCase);
                    if (token != null)
                    {
                        var value = token.Value.ToObject(prop.PropertyType, serializer);
                        prop.SetValue(obj, value);
                        break;
                    }
                }
            }

            return obj;
        }
        */

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        private static string ToCamelCase(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

        private static string ToSnakeCase(string s) =>
            string.Concat(s.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    }
}
