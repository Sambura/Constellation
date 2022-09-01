﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Core;

namespace UnityCore
{
    /// <summary>
    /// Serializes objects, using ConfigProperty attributes as metadata
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
                if (property.GetCustomAttribute<ConfigProperty>() == null) continue;
                JsonSerializerUtility.SerializeDefault(json, property.Name, property.GetValue(obj));
            }

            JsonSerializerUtility.RemoveLastComma(json);
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
                else if (type == typeof(string)) { json.Append('"'); json.Append((string)obj); json.Append('"'); }
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

                JsonSerializerUtility.RemoveLastComma(json);
                json.Append(']');
            } else
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
	/// Utility methods for above classes
	/// </summary>
	public static class JsonSerializerUtility
    {
        public static IReadOnlyDictionary<Type, IJsonPropertySerializer> CustomSerializers { get; private set; }

        /// <summary>
        /// Each even index has an opening brace, each next index has respective closing brace
        /// </summary>
        private const string BracePairs = "{}[]\"\"";

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
            // remove all pretty print chars
            json = json.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            // check curly braces
            if (json[0] != '{' || json[json.Length - 1] != '}')
                throw new JsonSerializerException("Invalid json format: opening and/or closing curly brace not found");
            // remove curly braces
            json = json.Substring(1, json.Length - 2);
            // Allocate dictionary
            Dictionary<string, string> properties = new Dictionary<string, string>();
            Stack<char> bracesStack = new Stack<char>();

            string propertyName = null;
            int lastIndex = 0;
            for (int index = 0; index < json.Length; index++)
            {
                char c = json[index];
                bool entryClosed = false;

                if (bracesStack.Count > 0 && bracesStack.Peek() == '"')
                {
                    if (c == '\\') index++;
                    if (c != '"') continue;

                    bracesStack.Pop();
                    entryClosed = true;

                    if (bracesStack.Count == 0 && propertyName == null)
                    {
                        if (index != json.Length - 1 && json[index + 1] != ':')
                            throw new JsonSerializerException("Invalid json format: missing colon");
                    }
                }

                switch (c)
                {
                    case '"':
                    case '{':
                    case '[':
                        if (entryClosed) break;
                        if (bracesStack.Count == 0) lastIndex = c == '"' ? index + 1 : index;
                        bracesStack.Push(c);
                        continue;
                    case '}':
                        if (bracesStack.Peek() != '{') throw new JsonSerializerException("Invalid json format: braces mismatch");
                        bracesStack.Pop();
                        entryClosed = true;
                        break;
                    case ']':
                        if (bracesStack.Peek() != '[') throw new JsonSerializerException("Invalid json format: braces mismatch");
                        bracesStack.Pop();
                        entryClosed = true;
                        break;
                    case ',':
                        entryClosed = true;
                        break;
                }

                if (entryClosed || index == json.Length - 1)
                {
                    if (bracesStack.Count != 0) continue;
                    if (index == json.Length - 1) index = json.Length;

                    bool getClosing = (c == '}' || c == ']') && index < json.Length;
                    string entry = json.Substring(lastIndex, index - lastIndex + (getClosing ? 1 : 0));
                    if (getClosing && json[index + 1] == ',') index++;

                    if (propertyName == null)
                    {
                        propertyName = entry;
                        index++; // colon
                    }
                    else
                    {
                        properties.Add(propertyName, entry);
                        propertyName = null;
                    }

                    lastIndex = index + 1;
                }
            }

            return properties;
        }

        public static List<string> GetArrayElements(string json)
        {
            // remove all pretty print chars
            json = json.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            // check square braces
            if (json[0] != '[' || json[json.Length - 1] != ']')
                throw new JsonSerializerException("Invalid json format: opening and/or closing square bracket not found");
            // remove square braces
            json = json.Substring(1, json.Length - 2);
            // Allocate dictionary
            List<string> elements = new List<string>();
            Stack<char> bracesStack = new Stack<char>();

            int lastIndex = 0;
            for (int index = 0; index < json.Length; index++)
            {
                char c = json[index];
                bool entryClosed = false;

                if (bracesStack.Count > 0 && bracesStack.Peek() == '"')
                {
                    if (c == '\\') index++;
                    if (c != '"') continue;

                    bracesStack.Pop();
                    entryClosed = true;
                }

                switch (c)
                {
                    case '"':
                    case '{':
                    case '[':
                        if (entryClosed) break;
                        if (bracesStack.Count == 0) lastIndex = c == '"' ? index + 1 : index;
                        bracesStack.Push(c);
                        continue;
                    case '}':
                        if (bracesStack.Peek() != '{') throw new JsonSerializerException("Invalid json format: braces mismatch");
                        bracesStack.Pop();
                        entryClosed = true;
                        break;
                    case ']':
                        if (bracesStack.Peek() != '[') throw new JsonSerializerException("Invalid json format: braces mismatch");
                        bracesStack.Pop();
                        entryClosed = true;
                        break;
                    case ',':
                        entryClosed = true;
                        break;
                }

                if (entryClosed || index == json.Length - 1)
                {
                    if (bracesStack.Count != 0) continue;
                    if (index == json.Length - 1) index = json.Length;

                    bool getClosing = (c == '}' || c == ']') && index < json.Length;
                    string entry = json.Substring(lastIndex, index - lastIndex + (getClosing ? 1 : 0));
                    if (getClosing && json[index + 1] == ',') index++;

                    elements.Add(entry);

                    lastIndex = index + 1;
                }
            }

            return elements;
        }

        /// <summary>
        /// This function accepts a json string. It analyzes only the content inside
        /// the first pair of square/curly brackets. It deletes these braces, returning
        /// a list of elements that were stored in this pair of braces.
        /// Example: this input:
        /// {
        ///     "name": "foo",
        ///     "value": "fee"
        /// }
        /// will produce this output:
        /// List<string> {
        ///     "\"name\":\"foo\"",
        ///     "\"value\":\"fee\""
        /// }
        /// 
        /// Note that all formatting is removed after this function is used
        /// 
        /// \\\\\\\\\
        /// 
        /// The function is just a concept, and is not implemented yet
        /// </summary>
        public static List<string> GetElements(string json)
        {
            // remove all pretty print chars
            json = json.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            // check first brace
            if (json[0] != '[' && json[0] != '{')
                throw new JsonSerializerException("Invalid json format: an opening curly or square brace missing.");

            // To remake (it does not work)
            // remove square braces
            json = json.Substring(1, json.Length - 2);
            // Allocate dictionary
            List<string> elements = new List<string>();
            Stack<char> bracesStack = new Stack<char>();

            int lastIndex = 0;
            for (int index = 0; index < json.Length; index++)
            {
                char c = json[index];
                bool entryClosed = false;

                if (bracesStack.Count > 0 && bracesStack.Peek() == '"')
                {
                    if (c == '\\') index++;
                    if (c != '"') continue;

                    bracesStack.Pop();
                    entryClosed = true;
                }

                switch (c)
                {
                    case '"':
                    case '{':
                    case '[':
                        if (entryClosed) break;
                        if (bracesStack.Count == 0) lastIndex = c == '"' ? index + 1 : index;
                        bracesStack.Push(c);
                        continue;
                    case '}':
                        if (bracesStack.Peek() != '{') throw new JsonSerializerException("Invalid json format: braces mismatch");
                        bracesStack.Pop();
                        entryClosed = true;
                        break;
                    case ']':
                        if (bracesStack.Peek() != '[') throw new JsonSerializerException("Invalid json format: braces mismatch");
                        bracesStack.Pop();
                        entryClosed = true;
                        break;
                    case ',':
                        entryClosed = true;
                        break;
                }

                if (entryClosed || index == json.Length - 1)
                {
                    if (bracesStack.Count != 0) continue;
                    if (index == json.Length - 1) index = json.Length;

                    bool getClosing = (c == '}' || c == ']') && index < json.Length;
                    string entry = json.Substring(lastIndex, index - lastIndex + (getClosing ? 1 : 0));
                    if (getClosing && json[index + 1] == ',') index++;

                    elements.Add(entry);

                    lastIndex = index + 1;
                }
            }

            return elements;
        }

        public static void RemoveLastComma(StringBuilder json)
		{
            if (json[json.Length - 1] == ',') json.Remove(json.Length - 1, 1);
		}

        public static void SerializeDefault(StringBuilder json, string name, object value)
		{
            PrintPropertyName(json, name);
            json.Append(DefaultJsonSerializer.Default.ToJson(value, false));
            json.Append(',');
        }

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
}