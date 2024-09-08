using System;

namespace Core.Json
{
    public class JsonSerializerException : Exception
    {
        public JsonSerializerException(string message) : base(message) { }

        public JsonSerializerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
