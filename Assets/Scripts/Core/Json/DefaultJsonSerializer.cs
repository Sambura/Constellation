using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System;
using System.Globalization;

namespace Core.Json
{
    [Flags]
    public enum JsonSerializerFlags
    {
        Default = SerializeFields | SerializeProperties,
        SerializeFields = 1,
        SerializeProperties = 2,
        SerializeReadonlyProperties = 4,
        SerializeNonPublicMembers = 8,
    }

    /// <summary>
    /// Default serializer, that serializes anything, but possibly wrong
    /// Internally uses other IJsonSerializer implementations, if any exist
    /// </summary>
    public class DefaultJsonSerializer : IJsonPropertySerializer<object>
    {
        public static DefaultJsonSerializer Default { get; } = new DefaultJsonSerializer();

        private static readonly Type[] ParseMethodArguments = new Type[] { typeof(string) };

        private readonly Dictionary<Type, JsonSerializerFlags> _typeSpecificFlags = new Dictionary<Type, JsonSerializerFlags>();

        public Dictionary<string, object> CustomParameters { get; private set; } = new Dictionary<string, object>();

        private string FormatFloat(string value)
        {
            if (value.Contains(".")) return value;
            return value + ".0";
        }

        // TODO: detect serialization cycles (when object A has object B as its property, and B has A as its property)
        public string ToJson(object obj)
        {
            if (obj == null) return "null";

            Type type = obj.GetType();
            var customSerializer = JsonSerializerUtility.GetSuperSerializer(type, typeof(object));
            if (customSerializer != null) return customSerializer.ToJson(obj);

            StringBuilder json = new StringBuilder();

            if (type.IsPrimitive)
            {
                if (type == typeof(bool)) { json.Append(obj.ToString().ToLowerInvariant()); }
                else if (type == typeof(float)) { json.Append(FormatFloat(((float)obj).ToString("G9", CultureInfo.InvariantCulture))); }
                else if (type == typeof(double)) { json.Append(FormatFloat(((double)obj).ToString("G17", CultureInfo.InvariantCulture))); }
                else { json.Append(obj.ToString()); }
            }
            else if (type.IsArray)
            {
                Array array = obj as Array;

                json.Append('[');

                foreach (object item in array)
                {
                    json.Append(ToJson(item));
                    json.Append(',');
                }

                JsonSerializerUtility.StripComma(json);
                json.Append(']');
            }
            else
            {
                JsonSerializerUtility.BeginObject(json);
                MemberInfo[] members = type.GetMembers();
                JsonSerializerFlags flags = JsonSerializerFlags.Default;
                if (_typeSpecificFlags.ContainsKey(type)) flags = _typeSpecificFlags[type];
                bool requirePublic = !flags.HasFlag(JsonSerializerFlags.SerializeNonPublicMembers);
                bool requireWriteable = !flags.HasFlag(JsonSerializerFlags.SerializeReadonlyProperties);

                foreach (MemberInfo member in members)
                {
                    NoJsonSerializationAttribute config = member.GetCustomAttribute<NoJsonSerializationAttribute>();
                    if (config is { } && !config.AllowToJson) continue;

                    switch (member.MemberType)
                    {
                        // TODO: think about disabling serialization of deprecated members (make it a flag?)
                        case MemberTypes.Field:
                            if (!flags.HasFlag(JsonSerializerFlags.SerializeFields)) continue;
                            FieldInfo field = (FieldInfo)member;
                            if (requirePublic && !field.IsPublic) continue;
                            JsonSerializerUtility.SerializeDefault(json, field.Name, field.GetValue(obj));
                            break;
                        case MemberTypes.Property:
                            if (!flags.HasFlag(JsonSerializerFlags.SerializeProperties)) continue;
                            PropertyInfo property = (PropertyInfo)member;
                            // TODO: what do we do with indexable properties?
                            if (property.GetIndexParameters().Length > 0) continue;
                            if (!property.CanRead) continue;
                            if (requirePublic && !property.GetMethod.IsPublic) continue;
                            if (requireWriteable && !property.CanWrite) continue;
                            if (requireWriteable && requirePublic && !property.SetMethod.IsPublic) continue;
                            JsonSerializerUtility.SerializeDefault(json, property.Name, property.GetValue(obj));
                            break;
                        default:
                            continue;
                    }
                }
                JsonSerializerUtility.EndObject(json);
            }

            return json.ToString();
        }

        public object FromJson(string json, Type type, bool ignoreUnknownProperties = false)
        {
            if (type == null) throw new ArgumentNullException("type");

            var customSerializer = JsonSerializerUtility.GetSuperSerializer(type, typeof(object));
            if (customSerializer != null) return customSerializer.FromJson(json, type);

            if (type == typeof(float)) { // We use invariant culture for floats to let parser know we use `0.0` rather than `0,0` 
                if (float.TryParse(json, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return value;
                throw new JsonSerializerException($"Failed to parse {json} as {type}");
            } else if (type == typeof(double)) {
                if (double.TryParse(json, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) return value;
                throw new JsonSerializerException($"Failed to parse {json} as {type}");
            } else
            if (type.IsPrimitive)
            {
                MethodInfo parser = type.GetMethod("Parse", ParseMethodArguments);
                if (parser == null)
                    throw new ArgumentException($"Handling of {type.FullName} type is not supported by {nameof(DefaultJsonSerializer)}");

                try
                {
                    return parser.Invoke(null, new object[] { json });
                }
                catch (Exception e)
                {
                    throw new JsonSerializerException($"Failed to parse {json} as {type}", e);
                }
            }
            else if (type.IsArray)
            {
                List<string> elements = JsonSerializerUtility.GetArrayElements(json);
                Type elementType = type.GetElementType();
                Array array = Array.CreateInstance(elementType, elements.Count);

                for (int i = 0; i < elements.Count; i++)
                    array.SetValue(FromJson(elements[i], elementType), i);

                return array;
            }

            object newObj = Activator.CreateInstance(type);
            var properties = JsonSerializerUtility.GetProperties(json);

            MemberInfo[] members = type.GetMembers();

            foreach (var jsonProperty in properties)
            {
                MemberInfo member = members.FirstOrDefault(x => x.Name == jsonProperty.Key);
                NoJsonSerializationAttribute config = member?.GetCustomAttribute<NoJsonSerializationAttribute>();
                if (config is { } && !config.AllowFromJson) continue;

                if (member is null)
                {
                    if (ignoreUnknownProperties) continue;
                    throw new JsonSerializerException($"Could not find member {jsonProperty.Key} on type {type}");
                }

                try
                {
                    switch (member.MemberType)
                    {
                        case MemberTypes.Field:
                            FieldInfo field = member as FieldInfo;
                            field.SetValue(newObj, FromJson(jsonProperty.Value, field.FieldType));
                            break;
                        case MemberTypes.Property:
                            PropertyInfo property = member as PropertyInfo;
                            property.SetValue(newObj, FromJson(jsonProperty.Value, property.PropertyType));
                            break;
                        default:
                            throw new JsonSerializerException($"Unexpected member type {member.MemberType}");
                    }
                }
                catch (JsonSerializerException e)
                {
                    throw new JsonSerializerException($"Failed to deserialize json property \"{member.Name}\": {e.Message}", e);
                }
            }

            return newObj;
        }

        public T FromJson<T>(string json, bool ignoreUnknownProperties = false) {
            object result = FromJson(json, typeof(T), ignoreUnknownProperties);
            return (T)result;
        }

        /// <summary>
        /// Deserializes the property value from the json string. Highly ineffective performance-wise
        /// </summary>
        /// <typeparam name="T">Property type to deserialize. Use string if type is unknown</typeparam>
        /// <param name="json">Input json</param>
        /// <param name="propertyName">Name of the property to deserialize</param>
        /// <param name="result">Destination for deserialized object</param>
        /// <returns>true if property was found, false otherwise</returns>
        public bool ReadJsonProperty<T>(string json, string propertyName, out T result)
        {
            var properties = JsonSerializerUtility.GetProperties(json);
            if (properties.TryGetValue(propertyName, out string value))
            {
                result = (T)Default.FromJson(value, typeof(T));
                return true;
            }

            result = default;
            return false;
        }

        public void FromJsonOverwrite(string json, object obj, bool ignoreUnknownProperties = false)
        {
            throw new NotImplementedException();
        }

        public void RegisterTypeSerializationFlags(Type type, JsonSerializerFlags flags)
        {
            _typeSpecificFlags[type] = flags;
        }
    }
}
