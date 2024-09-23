namespace ConfigSerialization
{
    public class RadioButtonsProperty : ConfigProperty
    {
        public string[] RadioNames { get; }
        public int[] RadioIndices { get; }

        public RadioButtonsProperty(string[] radioNames, int[] radioIndices = null, bool hasEvent = true) : base(hasEvent: hasEvent)
        {
            RadioNames = radioNames;
            RadioIndices = radioIndices;
        }
    }
}
