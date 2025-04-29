using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

namespace Core.Json
{
    /// <summary>
    /// Utility methods for Json serialization / deserialization used by DefaultJsonSerializer
    /// </summary>
    public static class JsonSerializerUtility
    {
        public static IReadOnlyDictionary<Type, IJsonPropertySerializer> CustomSerializers { get; private set; }
        public static IReadOnlyDictionary<Type, Type> CustomGenericSerializers { get; private set; }

        public const string PrettyCharacters = " \t\n\r";

        /// <summary>
        /// Finds all classes in this assembly that implement IJsonPropertySerializer<>
        /// </summary>
        public static Dictionary<Type, IJsonPropertySerializer> RefreshCustomSerializers()
        {
            Dictionary<Type, IJsonPropertySerializer> serializers = new Dictionary<Type, IJsonPropertySerializer>();
            Dictionary<Type, Type> genericSerializers = new Dictionary<Type, Type>();

            var allTypes = Assembly.GetAssembly(typeof(JsonSerializerUtility)).GetTypes();

            foreach (var type in allTypes)
            {
                if (type.IsAbstract || type.IsInterface) continue;
                var inface = type.GetInterfaces().FirstOrDefault(x => x.IsInterface && x.IsGenericType);
                if (inface == null) continue;
                if (inface.GetGenericTypeDefinition() != typeof(IJsonPropertySerializer<>)) continue;
                Type serializerType = inface.GenericTypeArguments[0];

                if (type.ContainsGenericParameters)
                {
                    serializerType = serializerType.GetGenericTypeDefinition();
                    genericSerializers.Add(serializerType, type);
                } else
                {
                    object serializer = type.GetConstructor(Type.EmptyTypes).Invoke(null);
                    serializers.Add(serializerType, serializer as IJsonPropertySerializer);
                }
            }

            CustomSerializers = serializers;
            CustomGenericSerializers = genericSerializers;
            return serializers;
        }

        static JsonSerializerUtility() { RefreshCustomSerializers(); }

        /// <summary>
        /// Turns a json string into a dictionary of property names mapped to their values as strings
        /// </summary>
        /// <param name="json">Json string, any valid formatting allowed</param>
        /// <returns>Dictionary mapping property name strings to property value strings</returns>
        /// <exception cref="JsonSerializerException">Throws an exception on parsing failure</exception>
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

        /// <summary>
        /// Completely parses the json down to a tree of properties and their values. 
        /// Does not perform value deserialization.
        /// </summary>
        /// <param name="json">A valid input json string</param>
        /// <param name="keepRawNodeValue">When false, only leaf nodes in the tree will have a non-null 
        ///     Value field. When true, all nodes will have their raw json representations stored in 
        ///     them as well</param>
        /// <returns>JsonTree representation of the json. If input string only contains whitespaces, returns null</returns>
        public static JsonTree ToJsonTree(string json, bool keepRawNodeValue = false) {
            try { System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack(); }
            catch (InsufficientExecutionStackException) { 
                throw new ArgumentException("Provided json is too deep");
            }

            int startIndex = SkipPrettyChars(json, 0);
            if (startIndex >= json.Length) return null;
            if (json[startIndex] != '{') return new JsonTree() { Value = Compress(json) };
            int endIndex = FindClosingBrace(json, startIndex, '}', throwOnError: true);

            JsonTree tree = new JsonTree() { Properties = new Dictionary<string, JsonTree>() };
            if (keepRawNodeValue) tree.Value = json.Substring(startIndex, endIndex - startIndex + 1);
            Dictionary<string, string> properties = GetProperties(json);

            foreach (KeyValuePair<string, string> property in properties) {
                tree.Properties.Add(property.Key, ToJsonTree(property.Value, keepRawNodeValue));
            }

            return tree;
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

        public static void BeginObject(StringBuilder json) { json.Append('{'); }

        public static void EndObject(StringBuilder json) { StripComma(json); json.Append('}'); }

        public static void BeginArray(StringBuilder json) { json.Append('['); }

        public static void EndArray(StringBuilder json) { StripComma(json); json.Append(']'); }

        /// <summary>
        /// Prints a property to json and puts a comma given property's string representation. Format: `name: value,`.
        /// Use SerializeDefault function if your value is not serialized to json.
        /// </summary>
        public static void PrintProperty(StringBuilder json, string name, string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            PrintPropertyName(json, name);
            json.Append(value);
            json.Append(',');
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
            string propertyName = json.Substring(openingQuote + 1, closingQuote - openingQuote - 1);
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
            throw new JsonSerializerException($"Expected a comma after `{valueType}` property value");
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
        /// In case of failure, a JsonSerializerException is thrown, or -1 is returned, depending on throwOnError argument
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
        /// character to look for. Backslash escape could be optionally disabled. Start index should point to the
        /// opening brace/quote. In case of failure, -1 is returned.
        /// <br></br>
        /// Example: given string `"hello \"world\""` and startIndex = 0, function will return 16, the index of last quote
        /// </summary>
        public static int FindQuotedTokenEnd(string json, int startIndex, char closingChar = '"', bool enableEscaping = true, bool throwOnError = true)
        {
            for (int index = startIndex + 1; index < json.Length;)
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
        public static void StripComma(StringBuilder json)
        {
            if (json[json.Length - 1] == ',') json.Remove(json.Length - 1, 1);
        }

        /// <summary>
        /// Serialize a given object to json using default serializer, putting a comma at the end
        /// </summary>
        public static void SerializeDefault(StringBuilder json, string name, object value) => 
            PrintProperty(json, name, DefaultJsonSerializer.Default.ToJson(value));

        /// <summary>
        /// Writes the given property name in the StringBuilder in a format <b>"propertyName":</b>
        /// </summary>
        public static void PrintPropertyName(StringBuilder json, string name)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            json.Append('"');
            json.Append(name);
            json.Append("\":");
        }

        public static IJsonPropertySerializer GetSuperSerializer(Type targetType, Type baseSerializer)
        {
            Type type = targetType;

            while (type != baseSerializer)
            {
                if (CustomSerializers.TryGetValue(type, out IJsonPropertySerializer serializer))
                    return serializer;

                if (CustomGenericSerializers.TryGetValue(type, out Type serializerType))
                {
                    Type constructedType = serializerType.MakeGenericType(targetType.GetGenericArguments());
                    return Activator.CreateInstance(constructedType) as IJsonPropertySerializer;
                }

                type = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type.BaseType;
                if (type == null) throw new Exception("Null base type reached");
            }

            return null;
        }

        public static string Prettify(string json)
        {
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

        /// <summary>
        /// De-prettifies the json by removing extra spaces / tabs / newlines.
        /// Advanced compression techniques were not implemented as of now.
        /// </summary>
        public static string Compress(string json)
        {
            StringBuilder compressed = new StringBuilder(json.Length);
            // current base: first uncopied character, index - current position
            int currentBase = 0, index = 0;

            while (index < json.Length)
            {
                int increment = 1;
                index = FindCharOrPrettyChar(json, index, '"');
                if (index < json.Length && json[index] == '"')
                {
                    index = FindQuotedTokenEnd(json, index) + 1;
                    increment = 0;
                }

                compressed.Append(json.Substring(currentBase, index - currentBase));
                index = currentBase = index + increment;
            }
            if (currentBase < json.Length) compressed.Append(json.Substring(currentBase, index - currentBase));

            return compressed.ToString();
        }
    }

    public class JsonTree {
        public Dictionary<string, JsonTree> Properties;
        public string Value;
        public bool IsLeaf => Properties == null;

        public JsonTree this[string propertyName]
        {
            get => Properties?.GetValueOrDefault(propertyName);
            set => Properties[propertyName] = value;
        }

        /// <summary>
        /// Makes a json string using JsonTree data. `Value` field is not used for non-leaf nodes.
        /// The result is a minimal json representation, no prettyfication is performed.
        /// </summary>
        public string ToJson() {
            if (IsLeaf) return Value;

            StringBuilder json = new StringBuilder();
            JsonSerializerUtility.BeginObject(json);
            foreach (KeyValuePair<string, JsonTree> property in Properties)
                JsonSerializerUtility.PrintProperty(json, property.Key, property.Value.ToJson());
            JsonSerializerUtility.EndObject(json);
            
            return json.ToString();
        }

        public void Add(string key, JsonTree value) {
            Properties ??= new Dictionary<string, JsonTree>();
            
            Properties.Add(key, value);
        }
    }
}
