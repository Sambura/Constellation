using System;

namespace ConfigSerialization.Structuring
{
    public class ConfigMemberOrderAttribute : Attribute
    {
        public int DisplayIndex { get; protected set; }

        public ConfigMemberOrderAttribute(int index) { DisplayIndex = index; }
    }
}
