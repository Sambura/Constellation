using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Core.Json;
using ConfigSerialization;

namespace UnityCore
{
    /// <summary>
    /// Serializes objects, using ConfigProperty attributes as metadata
    /// If a property does not have a getter/setter or both, it is ignored
    /// All subobjects are serialized using implementations of IJsonPropertySerializer's
    /// </summary>
    public static class ConfigJsonSerializer
    {
        public static string ConfigToJson(object obj, bool prettyPrint = false)
        {
            if (obj == null) return "null";

            StringBuilder json = new StringBuilder();
            json.Append('{');

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<ConfigProperty>() is null) continue;
                if (!property.CanWrite || !property.CanRead) continue;
                JsonSerializerUtility.SerializeDefault(json, property.Name, property.GetValue(obj));
            }

            JsonSerializerUtility.StripComma(json);
            json.Append('}');

            return JsonSerializerUtility.Prettify(json.ToString(), prettyPrint);
        }

        /// <summary>
        /// Overwrite object's properties from the json string. Only properties that have ConfigProperty
        /// attribute are overwritten. If the json string contains properties that are not present on the
        /// object, they are simply ignored. The properties are overwritten using property setters.
        /// </summary>
        /// <param name="json">Json string with new property values</param>
        /// <param name="obj">Object whose properties should be overwritten</param>
        /// <returns>The number of succesfully overwritten properties</returns>
        public static int OverwriteConfigFromJson(string json, object obj)
        {
            Dictionary<string, string> data = JsonSerializerUtility.GetProperties(json);
            int deserealized = 0;

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (data.TryGetValue(property.Name, out string value))
                {
                    Type type = property.PropertyType;
                    ConfigProperty attribute = property.GetCustomAttribute<ConfigProperty>();
                    if (attribute == null) { Debug.LogWarning("Invalid property deserialization attempt"); continue; }
                    property.SetValue(obj, DefaultJsonSerializer.Default.FromJson(value, type));
                    deserealized++;
                }
            }

            return deserealized;
        }

        // Since ConfigJsonSerializer contains Unity-dependent code right now, we might as well initialize the default serializer here
        // with Unity-specific type serialization
        static ConfigJsonSerializer()
        {
            // a,r,g,b are all public fields. Serializing properties results in an infinite recursion
            DefaultJsonSerializer.Default.RegisterTypeSerializationFlags(typeof(UnityEngine.Color), JsonSerializerFlags.SerializeFields);
        }
    }
}
