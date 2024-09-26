using System;

namespace Core.Json
{
    /// <summary>
    /// Attribute that disables serialization / deserialization of the target field or property when
    /// using <see cref="Core.Json.DefaultJsonSerializer"/>. Can optionally allow either serialization
    /// or deserialization, without allowing both.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NoJsonSerializationAttribute : Attribute
    {
        public bool AllowToJson { get; set; } = false;
        public bool AllowFromJson { get; set; } = false;

        public NoJsonSerializationAttribute() { }
    }
}
