using System;

namespace Core.Json
{
    public interface IJsonPropertySerializer<T> : IJsonPropertySerializer { }

    public interface IJsonPropertySerializer
    {
        public string ToJson(object obj);
        public object FromJson(string json, Type type, bool ignoreUnknownProperties = false);
        public void FromJsonOverwrite(string json, object obj, bool ignoreUnknownProperties = false);
    }
}