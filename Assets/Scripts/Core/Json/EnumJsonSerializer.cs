using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Json
{
    public class EnumJsonSerializer : IJsonPropertySerializer<Enum>
    {
        public string ToJson(object obj)
        {
            Enum enumValue = obj as Enum;
            
            return DefaultJsonSerializer.Default.ToJson(enumValue.ToString());
        }

        public object FromJson(string json, Type type, bool ignoreUnknownProperties = false)
        {
            if (!type.IsEnum) throw new ArgumentException("Type should be an Enum");

            // Old version serialized enums as `{ "value__": <value> }`
            if (json.Contains("{"))
            {
                string valuePropertyName = "value__";
                Dictionary<string, string> properties = JsonSerializerUtility.GetProperties(json);
                if (properties.Count < 1 || !properties.ContainsKey(valuePropertyName))
                    throw new JsonSerializerException($"Failed to deserialize {json} as an Enum {type} : expected {valuePropertyName} property");
                if (properties.Count > 1 && !ignoreUnknownProperties)
                    throw new JsonSerializerException($"Failed to deserialize {json} as an Enum {type} : unexpected property {properties.First(x => x.Key != valuePropertyName)}");

                int enumValue = (int)DefaultJsonSerializer.Default.FromJson(properties[valuePropertyName], typeof(int), ignoreUnknownProperties);
                if (Enum.IsDefined(type, enumValue))
                    return Enum.ToObject(type, enumValue);

                throw new JsonSerializerException($"Failed to deserialize {properties[valuePropertyName]} as an Enum {type} : invalid value");
            }

            string value = DefaultJsonSerializer.Default.FromJson(json, typeof(string), ignoreUnknownProperties) as string;

            try {
                return Enum.Parse(type, value, false);
            }
            catch (Exception e) { 
                throw new JsonSerializerException($"Failed to deserialize {json} as an Enum {type}", e);
            }
        }

        public void FromJsonOverwrite(string json, object obj, bool ignoreUnknownProperties = false)
        {
            throw new NotImplementedException();
        }
    }
}
