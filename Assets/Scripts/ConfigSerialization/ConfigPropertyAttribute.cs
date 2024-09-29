using System;

namespace ConfigSerialization
{
    public class ConfigProperty : Attribute
    {
        public string Name { get; set; }
        public bool HasEvent { get; }
        public bool? IsPollingAllowed { get; private set; }
        public bool AllowPolling { get => false; set => IsPollingAllowed = value; }    

        public ConfigProperty(string name = null, bool hasEvent = true)
        {
            Name = name;
            HasEvent = hasEvent;
        }
    }
}
