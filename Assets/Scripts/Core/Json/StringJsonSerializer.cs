using System.Text;
using System;

namespace Core.Json
{
    /// <summary>
    /// Default Json serializer for strings. Is used by DefaultJsonSerializer
    /// </summary>
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

        public object FromJson(string json, Type type, bool ignoreUnknownProperties = false)
        {
            if (type != typeof(string)) throw new ArgumentException("Type should be string");
            if (json == "null") return null;
            if (json[0] != '"' || json[json.Length - 1] != '"')
                throw new JsonSerializerException($"Failed to deserialize {json} as string : no quotes");
            return json.Substring(1, json.Length - 2).Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        public void FromJsonOverwrite(string json, object obj, bool ignoreUnknownProperties = false)
        {
            throw new NotImplementedException();
        }
    }
}
