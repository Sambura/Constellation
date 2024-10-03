using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Json
{
    public class ListJsonSerializer<T> : IJsonPropertySerializer<List<T>>
    {
        public string ToJson(object obj)
        {
            List<T> list = obj as List<T>;
            if (list is null) return "null";

            StringBuilder json = new StringBuilder();
            JsonSerializerUtility.BeginArray(json);
            foreach (T item in list)
            {
                json.Append(DefaultJsonSerializer.Default.ToJson(item));
                json.Append(',');
            }
            JsonSerializerUtility.EndArray(json);

            return json.ToString();
        }

        public object FromJson(string json, Type type, bool ignoreUnknownProperties = false)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>)) 
                throw new ArgumentException("Type should be System.Collections.Generic.List<>");

            if (json == "null") return null;
            
            List<string> elements = JsonSerializerUtility.GetArrayElements(json);
            List<T> list = new List<T>(elements.Count);

            foreach (string element in elements)
            {
                list.Add((T)DefaultJsonSerializer.Default.FromJson(element, typeof(T), ignoreUnknownProperties));
            }

            return list;
        }

        public void FromJsonOverwrite(string json, object obj, bool ignoreUnknownProperties = false)
        {
            throw new NotImplementedException();
        }
    }
}
