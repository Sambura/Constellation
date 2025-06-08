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
                throw new JsonSerializerException($"Failed to read object's properties: expected `{{`, got `{json[openingBrace]}`", json: json);
            int closingBrace = FindClosingBrace(json, openingBrace, '}');
            if (SkipPrettyChars(json, closingBrace + 1) < json.Length)
                throw new JsonSerializerException($"Failed to read object's properties: expected EOF, got {json[SkipPrettyChars(json, closingBrace + 1)]}", json: json);

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
        /// <returns>JsonObject representation of the json. If input string only contains whitespaces, returns null</returns>
        public static JsonObject ToJsonObject(string json, bool keepRawNodeValue = false, bool compressionRequired = true) {
            try { System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack(); }
            catch (InsufficientExecutionStackException) { 
                throw new ArgumentException("Provided json is too deep");
            }

            if (compressionRequired) json = Compress(json);
            if (json.Length <= 0) return null;
            if (json[0] == '[') {
                return new JsonArray(
                    GetArrayElements(json).Select(x => ToJsonObject(x, keepRawNodeValue, false)).ToList(),
                    keepRawNodeValue ? json : null
                );
            }
            if (json[0] == '{') {
                return new JsonTree(
                    new Dictionary<string, JsonObject>(GetProperties(json).Select(
                        x => new KeyValuePair<string, JsonObject>(x.Key, ToJsonObject(x.Value, keepRawNodeValue, false)))),
                    keepRawNodeValue ? json : null
                );
            }

            return new JsonLeaf(json);
        }

        /// <summary>
        /// Get elements stored in a json array. The input json should start with `[` and end with `]`
        /// </summary>
        public static List<string> GetArrayElements(string json)
        {
            int openingBrace = SkipPrettyChars(json, 0);
            if (json[openingBrace] != '[')
                throw new JsonSerializerException($"Failed to read an array: expected `[`, got `{json[openingBrace]}`", json: json);
            int closingBrace = FindClosingBrace(json, openingBrace, ']');
            if (SkipPrettyChars(json, closingBrace + 1) < json.Length)
                throw new JsonSerializerException($"Failed to read an array: expected EOF, got {json[SkipPrettyChars(json, closingBrace + 1)]}", json: json);

            string arrayBody = json.Substring(openingBrace + 1, closingBrace - openingBrace - 1);
            List<string> elements = new List<string>();

            for (int index = 0; index < arrayBody.Length;)
                elements.Add(ReadPropertyValue(arrayBody, ref index));

            return elements;
        }

        /// <summary>      Prints an opening object character `{`      </summary>
        public static void BeginObject(StringBuilder json) { json.Append('{'); }
        /// <summary>    Prints a closing object character `}`, removing a trailing comma if required    </summary>
        public static void EndObject(StringBuilder json) { StripComma(json); json.Append('}'); }
        /// <summary>      Prints an opening array character `[`      </summary>
        public static void BeginArray(StringBuilder json) { json.Append('['); }
        /// <summary>    Prints a closing array character `]`, removing a trailing comma if required    </summary>
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
        /// Prints element as is and adds a comma at the end. Should be called after BeginArray or after another AppendArray
        /// </summary>
        public static void AppendArray(StringBuilder json, string element)
        {
            if (element is null) throw new ArgumentNullException(nameof(element));

            json.Append(element);
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

    public abstract class JsonObject
    {
        public abstract string Value { get; set; }
        public virtual bool IsLeaf { get; } = false;
        public virtual bool IsTree { get; } = false;
        public virtual bool IsArray { get; } = false;
        public virtual int Count { get; } = 1;
        public JsonObject this[string propertyName] {
            get => IsTree ? ToTree()[propertyName] : null;
            set { if (IsTree) ToTree()[propertyName] = value; }
        }
        public virtual void Remove(string propertyName) { ToTree().Remove(propertyName); }
        public virtual string ToJson() => Value;
        public virtual JsonTree ToTree() { if (!IsTree) throw new JsonSerializerException("The provided json is not a tree", Value); return (JsonTree)this; }
        public virtual JsonArray ToArray() { if (!IsArray) throw new JsonSerializerException("The provided json is not an array", Value); return (JsonArray)this; }
        public virtual JsonLeaf ToLeaf() { if (!IsLeaf) throw new JsonSerializerException("The provided json is not a leaf", Value); return (JsonLeaf)this; }
    }

    public class JsonTree : JsonObject {
        private string _value = null;
        private Dictionary<string, JsonObject> _properties = null;

        public override bool IsTree { get; } = true;
        public override string Value
        {
            get => _value ??= ToJson();
            set { _value = JsonSerializerUtility.Compress(value); _properties = null; }
        }
        public override int Count => Properties.Count;
        public Dictionary<string, JsonObject> Properties
        {
            get => _properties ??= JsonSerializerUtility.ToJsonObject(_value, false).ToTree().Properties;
            set {  _properties = value; _value = null; }
        }
        public new JsonObject this[string propertyName]
        {
            get => Properties.GetValueOrDefault(propertyName);
            set { Properties[propertyName] = value; _value = null; }
        }

        public override void Remove(string propertyName) {
            Properties.Remove(propertyName);
        }

        public override string ToJson() {
            if (_value is not null) return _value;

            StringBuilder json = new StringBuilder();
            JsonSerializerUtility.BeginObject(json);
            foreach (KeyValuePair<string, JsonObject> property in Properties)
                JsonSerializerUtility.PrintProperty(json, property.Key, property.Value.Value);
            JsonSerializerUtility.EndObject(json);
            
            return json.ToString();
        }

        public JsonTree(Dictionary<string, JsonObject> properties = null, string rawValue = null)
        {
            _properties = properties;
            _value = rawValue;
            if (_properties is null && _value is null) _properties = new Dictionary<string, JsonObject>();
        }
    }

    public class JsonArray : JsonObject
    {
        private string _value = null;
        private List<JsonObject> _elements = null;

        public override bool IsArray { get; } = true;
        public override string Value
        {
            get => _value ??= ToJson();
            set { _value = JsonSerializerUtility.Compress(value); _elements = null; }
        }
        public List<JsonObject> Elements
        {
            get => _elements ??= JsonSerializerUtility.ToJsonObject(_value, false).ToArray().Elements;
            set { _elements = value; _value = null; }
        }
        public JsonObject this[int index]
        {
            get => index < 0 || index >= Count ? null : Elements[index];
            set { Elements[index] = value; _value = null; }
        }
        public override int Count => Elements.Count;

        public override string ToJson()
        {
            if (_value is not null) return _value;

            StringBuilder json = new StringBuilder();
            JsonSerializerUtility.BeginArray(json);
            foreach (JsonObject element in Elements)
                JsonSerializerUtility.AppendArray(json, element.Value);
            JsonSerializerUtility.EndArray(json);

            return json.ToString();
        }

        public JsonArray(List<JsonObject> elements, string rawValue = null)
        {
            _elements = elements;
            _value = rawValue;
        }
    }

    public class JsonLeaf : JsonObject
    {
        private string _value = null;

        public override bool IsLeaf { get; } = true;
        public override string Value
        {
            get => _value ?? throw new JsonSerializerException("The leaf json object is null");
            set => _value = JsonSerializerUtility.Compress(value);
        }

        public JsonLeaf(string value) {
            Value = value;
        }
    }
}
