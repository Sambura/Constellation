using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Core;
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

            JsonSerializerUtility.RemoveLastIfComma(json);
            json.Append('}');

            return JsonSerializerUtility.Prettyfy(json.ToString(), prettyPrint);
        }

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
    }

    /// <summary>
    /// Default serializer, that serializes anything, but possibly wrong
    /// Internally uses other IJsonSerializer implementations, if any exist
    /// </summary>
    public class DefaultJsonSerializer : IJsonPropertySerializer<object>
    {
        public static readonly DefaultJsonSerializer Default = new DefaultJsonSerializer();

        private static readonly Type[] ParseMethodArguments = new Type[] { typeof(string) };

        public string ToJson(object obj, bool prettyPrint)
        {
            if (obj == null) return "null";

            Type type = obj.GetType();
            var customSerializer = JsonSerializerUtility.GetSuperSerializer(type, typeof(object));
            if (customSerializer != null) return customSerializer.ToJson(obj, prettyPrint);

            StringBuilder json = new StringBuilder();

            if (type.IsPrimitive)
            {
                if (type == typeof(bool)) { json.Append(obj.ToString().ToLowerInvariant()); }
                else if (type == typeof(float)) { json.Append(((float)obj).ToString("G9")); }
                else if (type == typeof(double)) { json.Append(((double)obj).ToString("G17")); }
                else { json.Append(obj.ToString()); }
            }
            else if (type.IsArray)
            {
                Array array = obj as Array;

                json.Append('[');

                foreach (object item in array)
                {
                    json.Append(ToJson(item, false));
                    json.Append(',');
                }

                JsonSerializerUtility.RemoveLastIfComma(json);
                json.Append(']');
            }
            else
            {
                json.Append(JsonUtility.ToJson(obj, prettyPrint));
            }

            return JsonSerializerUtility.Prettyfy(json.ToString(), false);
        }

        public object FromJson(string json, Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            var customSerializer = JsonSerializerUtility.GetSuperSerializer(type, typeof(object));
            if (customSerializer != null) return customSerializer.FromJson(json, type);

            if (type.IsPrimitive)
            {
                MethodInfo parser = type.GetMethod("Parse", ParseMethodArguments);
                if (parser == null)
                    throw new ArgumentException($"Handling of {type.FullName} type is not supported by BasicJsonSerializer");

                return parser.Invoke(null, new object[] { json });
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
            else return JsonUtility.FromJson(json, type);
        }

        public void FromJsonOverwrite(string json, object obj)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Utility methods for DefaultJsonSerializer & ConfigJsonSerializer
    /// </summary>
    public static class JsonSerializerUtility
    {
        public static IReadOnlyDictionary<Type, IJsonPropertySerializer> CustomSerializers { get; private set; }

        public const string PrettyCharacters = " \t\n\r";

        /// <summary>
        /// Finds all classes in this assembly that implement IJsonPropertySerializer<>
        /// </summary>
        public static Dictionary<Type, IJsonPropertySerializer> RefreshCustomSerializers()
        {
            Dictionary<Type, IJsonPropertySerializer> serializers = new Dictionary<Type, IJsonPropertySerializer>();

            var allTypes = Assembly.GetAssembly(typeof(JsonSerializerUtility)).GetTypes();

            foreach (var type in allTypes)
            {
                if (type.IsAbstract || type.IsInterface) continue;
                var inface = type.GetInterfaces().FirstOrDefault(x => x.IsInterface && x.IsGenericType);
                if (inface == null) continue;
                if (inface.GetGenericTypeDefinition() != typeof(IJsonPropertySerializer<>)) continue;

                object serializer = type.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
                serializers.Add(inface.GenericTypeArguments[0], serializer as IJsonPropertySerializer);
            }

            CustomSerializers = serializers;
            return serializers;
        }

        static JsonSerializerUtility() { RefreshCustomSerializers(); }

        public static Dictionary<string, string> GetProperties(string json)
        {
            int openingBrace = SkipPrettyChars(json, 0);
            if (json[openingBrace] != '{') 
                throw new JsonSerializerException($"Failed to read object's properties: expected `{{`, got `{json[openingBrace]}`");
            int closingBrace = FindClosingBrace(json, openingBrace, '}');
            if (SkipPrettyChars(json, closingBrace + 1) < json.Length)
                throw new JsonSerializerException($"Failed to read object's properties: expected EOF, got {json[SkipPrettyChars(json, closingBrace + 1)]}");

            string objectBody = json.Substring(openingBrace + 1, closingBrace - openingBrace - 1);

            Dictionary<string, string> properties = new Dictionary<string, string>();

            for (int index = 0; index < objectBody.Length;)
			{
                string name = ReadPropertyName(objectBody, ref index);
                string value = ReadPropertyValue(objectBody, ref index);
                properties.Add(name, value);
			}

            return properties;
        }

        public static List<string> GetArrayElements(string json)
		{
            int openingBrace = SkipPrettyChars(json, 0);
            if (json[openingBrace] != '[')
                throw new JsonSerializerException($"Failed to read an array: expected `[`, got `{json[openingBrace]}`");
            int closingBrace = FindClosingBrace(json, openingBrace, ']');
            if (SkipPrettyChars(json, closingBrace + 1) < json.Length)
                throw new JsonSerializerException($"Failed to read an array: expected EOF, got {json[SkipPrettyChars(json, closingBrace + 1)]}");

            string arrayBody = json.Substring(openingBrace + 1, closingBrace - openingBrace - 1);
            List<string> elements = new List<string>();

            for (int index = 0; index < arrayBody.Length;)
                elements.Add(ReadPropertyValue(arrayBody, ref index));

            return elements;
        }

        /// <summary>
        /// Skips all the spaces, tabs, and newline characters in the string
        /// Returns the index of first character that was not skipped
        /// </summary>
        public static int SkipPrettyChars(string json, int index)
		{
            while (index < json.Length && PrettyCharacters.Contains(json[index])) index++;
            return index;
		}

        /// <summary>
        /// Reads a property name from a json, and leaves index pointing at the first character after the colon `:`.
        /// Json should be un-prettified. Index should point at the first quote of property name.
        /// </summary>
        public static string ReadPropertyName(string json, ref int index)
		{
            int openingQuote = SkipPrettyChars(json, index);
            if (json[openingQuote] != '"') throw new JsonSerializerException($"Expected a quote (property name), got `{json[openingQuote]}`");
            int closingQuote = FindQuotedTokenEnd(json, openingQuote);
            index = SkipPrettyChars(json, closingQuote + 1);
            if (json[index++] != ':') throw new JsonSerializerException("Missing a colon after a property name");
            string propertyName = json.Substring(openingQuote + 1 , closingQuote - openingQuote - 1);
            return propertyName;
		}

        /// <summary>
        /// Reads a value of the property given the index of the first character of the value. The whole value (including
        /// surrounding braces/quotes, if any) is returned, and the index is updated to point to the next property name
        /// or end of stream.
        /// If the first character of the value is not `"`, `{` or `[`, the function will not detect any syntax errors
        /// </summary>
        public static string ReadPropertyValue(string json, ref int index)
		{
            int valueStart = SkipPrettyChars(json, index);
            char firstChar = json[valueStart];
            int valueEnd;
            string valueType;

            switch (firstChar)
			{
                case '"':
                    valueType = "string";
                    valueEnd = FindQuotedTokenEnd(json, valueStart);
                    break;
                case '{':
                    valueType = "object";
                    valueEnd = FindClosingBrace(json, valueStart, '}');
                    break;
                case '[': 
                    valueType = "array";
                    valueEnd = FindClosingBrace(json, valueStart, ']');
                    break;
                // Otherwise assume it is a number/bool, and read the next until the first comma / end of stream
                // If it is not a number/bool, but some illegal formatting - let something else handle that (probably)
                default:
                    valueType = "primitive";
                    valueEnd = FindCharOrPrettyChar(json, valueStart, ',') - 1;
                    break;
			}

            string value = json.Substring(valueStart, valueEnd - valueStart + 1);
            index = SkipPrettyChars(json, valueEnd + 1);
            if (index == json.Length || json[index++] == ',') return value;
            throw new JsonSerializerException($"Expected a comma after the {valueType}");
        }

        /// <summary>
        /// Searches the string for json pretty characters (space/tab/newline) and for the specified character.
        /// Returns the index of the first found character starting from the specified index (found character is either
        /// the target char or a pretty char). In case non of these are found, returns the length of the string.
        /// </summary>
        public static int FindCharOrPrettyChar(string json, int index, char target)
		{
            while (index < json.Length && json[index] != target && !PrettyCharacters.Contains(json[index])) index++;
            return index;
		}

        /// <summary>
        /// Finds the index of the closing brace considering the nesting and skipping quoted `"` parts, accounting for 
        /// backslash escaping. The arguments are the index of the opening brace and the character for a closing one.
        /// Does not work in case opening and closing brace is the same character (try using FindQuotedTokenEnd).
        /// In case of failre, a JsonSerializerException is thrown, or -1 is returned, depending on throwOnError argument
        /// </summary>
        public static int FindClosingBrace(string json, int startIndex, char closingChar, bool throwOnError = true)
		{
            char openingBrace = json[startIndex];
            int braceLevel = 0;
            for (int i = startIndex + 1; i < json.Length; i++)
			{
                if (json[i] == openingBrace) braceLevel++;
                else if (json[i] == '"') { i = FindQuotedTokenEnd(json, i, throwOnError: throwOnError); if (i < 0) return -1; }
                else if (json[i] == closingChar && 0 == braceLevel--) return i;
                
			}

            if (!throwOnError) return -1;
            throw new JsonSerializerException($"Did not found the matching brace {closingChar} for {openingBrace}");
		}

        /// <summary>
        /// Finds the index of the closing brace of a token, given the beginning of the token and the closing
        /// character to look for. Backslash espape could be optionally disabled. Start index should point to the
        /// opening brace/quote. In case of failre, -1 is returned.
        /// Example: given string `"hello \"world\""` and startIndex = 0, function will return 16, the index of last quote
        /// </summary>
        public static int FindQuotedTokenEnd(string json, int startIndex, char closingChar = '"', bool enableEscaping = true, bool throwOnError = true)
		{
            for (int index = startIndex + 1; index < json.Length; )
			{
                int nextClosing = json.IndexOf(closingChar, index);
                if (nextClosing <= 0)
                { // 0 shouldn't really happen, but if it does, it's likely a caller's fault
                    if (throwOnError) throw new JsonSerializerException("Unmatched quote or some unknown error");
                    return nextClosing;
                }
                if (!enableEscaping) return nextClosing;
                // even amount of backslashes means quote is not escaped, odd = escaped
                for (index = nextClosing - 1; json[index] == '\\'; index--) ;
                if ((nextClosing - index) % 2 == 0)
                {
                    index = nextClosing + 1;
                    continue;
                }
                return nextClosing;
			}

            return -1;
		}

        /// <summary>
        /// Removes the last character from the StringBuilder <b>only if</b> it is a comma `,`
        /// </summary>
        /// <param name="json"></param>
        public static void RemoveLastIfComma(StringBuilder json)
        {
            if (json[json.Length - 1] == ',') json.Remove(json.Length - 1, 1);
        }

        /// <summary>
        /// Serialize a given object to json using default serializer, putting a comma at the end
        /// </summary>
        public static void SerializeDefault(StringBuilder json, string name, object value)
        {
            PrintPropertyName(json, name);
            json.Append(DefaultJsonSerializer.Default.ToJson(value, false));
            json.Append(',');
        }

        /// <summary>
        /// Writes the given property name in the StringBuilder in a format <b>"propertyName":</b>
        /// </summary>
        public static void PrintPropertyName(StringBuilder json, string name)
        {
            json.Append('"');
            json.Append(name);
            json.Append("\":");
        }

        public static IJsonPropertySerializer GetSuperSerializer(Type type, Type baseSerializer)
        {
            while (type != baseSerializer)
            {
                if (CustomSerializers.TryGetValue(type, out IJsonPropertySerializer serializer))
                    return serializer;

                type = type.BaseType;
                if (type == null) throw new Exception("Null base type reached");
            }

            return null;
        }

        public static string Prettyfy(string json, bool prettify = true)
        {
            if (prettify == false) return json;

            // Below code is (supposed to be) effectively equivalent to this one:
            //
            // json = json.Replace("{", "{\n").Replace("}", "\n}")
            //     .Replace("{\n\n}", "{}").Replace("[", "[\n").Replace("]", "\n]")
            //     .Replace("[\n\n]", "[]").Replace(":", ": ").Replace(",", ",\n");
            //
            // Except I suppose it is faster/more efficient + ignores contents of quoted strings

            StringBuilder prettyJson = new StringBuilder(json.Length);
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                switch (c)
                {
                    case '[':
                    case '{':
                        prettyJson.Append(c);
                        if (json[i + 1] != ']' && json[i + 1] != '}')
                            prettyJson.Append('\n');
                        break;
                    case ']':
                    case '}':
                        prettyJson.Append('\n');
                        prettyJson.Append(c);
                        break;
                    case ':':
                        prettyJson.Append(": ");
                        break;
                    case ',':
                        prettyJson.Append(",\n");
                        break;
                    case '"':
                        int openingQuote = i;
                        i = FindQuotedTokenEnd(json, i, throwOnError: false);
                        if (i == -1) throw new JsonSerializerException("Failed to prettify the generated JSON");
                        prettyJson.Append(json.Substring(openingQuote, i - openingQuote + 1));
                        break;
                    default:
                        prettyJson.Append(c);
                        break;
                }
            }

            // Code section end

            json = prettyJson.ToString();
            prettyJson.Clear();

            int index = 0;
            int indentLevel = 0;
            int indentCount = 4;
            while (index < json.Length)
            {
                int nextOpeningBrace = MathUtility.MinPositive(json.IndexOf('{', index), json.IndexOf('[', index));
                int nextClosingBrace = MathUtility.MinPositive(json.IndexOf('}', index), json.IndexOf(']', index));

                if (nextClosingBrace < 0) break;
                int nextBrace = MathUtility.MinPositive(nextClosingBrace, nextOpeningBrace);
                bool opening = nextBrace == nextOpeningBrace;

                prettyJson.Append(json.Substring(index, nextBrace - index + 1).Replace("\n", "\n" + new string(' ', indentCount * indentLevel)));
                if (opening == false && indentLevel > 0)
                {
                    prettyJson.Remove(prettyJson.Length - indentCount - 1, indentCount);
                }
                indentLevel += opening ? 1 : -1;
                index = nextBrace + 1;
            }

            return prettyJson.ToString();
        }
    }

    public class JsonSerializerException : Exception
    {
        public JsonSerializerException(string message) : base(message) { }
    }

    public class StringJsonSerializer : IJsonPropertySerializer<string>
    {
        public string ToJson(object obj, bool prettyPrint)
        {
            if (obj == null) return "null";

            string str = obj as string; // TODO: Make this check in the base class/interface/idk
            if (str == null) throw new ArgumentException("The object to serialize is not of type string");

            StringBuilder json = new StringBuilder(str.Length * 2); // pretty arbitrary
            json.Append('"');
            json.Append(str.Replace("\\", "\\\\").Replace("\"", "\\\""));
            json.Append('"');

            return json.ToString();
        }

        public object FromJson(string json, Type type)
        {
            if (type != typeof(string)) throw new ArgumentException("Type should be string");
            if (json == "null") return null;
            return json.Substring(1, json.Length - 2).Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        public void FromJsonOverwrite(string json, object obj)
        {
            throw new NotImplementedException();
        }
    }
}