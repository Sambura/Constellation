using System;

namespace Core.Json
{
    public class NullableJsonSerializer<T> : IJsonPropertySerializer<Nullable<T>> where T : struct
    {
        // Default serialization works for nullables, we only need custom deserialization
        public string ToJson(object obj) => throw new NotImplementedException();

        public object FromJson(string json, Type type, bool ignoreUnknownProperties = false)
        {
            Type nullableType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (nullableType != typeof(Nullable<>)) throw new ArgumentException("Type should be System.Nullable<>");

            if (json == "null") return Activator.CreateInstance(type);

            Type valueType = type.GetGenericArguments()[0];
            object value = DefaultJsonSerializer.Default.FromJson(json, valueType);

            return Activator.CreateInstance(type, value);
        }

        public void FromJsonOverwrite(string json, object obj, bool ignoreUnknownProperties = false)
        {
            throw new NotImplementedException();
        }
    }
}
