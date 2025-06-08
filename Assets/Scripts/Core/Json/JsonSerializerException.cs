using System;

namespace Core.Json
{
    public class JsonSerializerException : Exception
    {
        public string JsonSource { get; private set; }

        public JsonSerializerException(string message, string json = null) : base(message) { JsonSource = json; }

        public JsonSerializerException(string message, Exception innerException, string json = null) : base(message, innerException) { JsonSource = json; }
    }
}
