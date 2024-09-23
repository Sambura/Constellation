namespace ConfigSerialization
{
    public class InputFieldPropertyAttribute : ConfigProperty
    {
        public float MinValue { get; }
        public float MaxValue { get; }
        public string InputFormatting { get; }
        public string InputRegex { get; }
        public int? RegexGroupIndex { get; }

        public InputFieldPropertyAttribute(float minValue = float.MinValue, float maxValue = float.MaxValue,
                            string inputFormatting = null, string inputRegex = null, int regexGroupIndex = -1, 
                            string name = null, bool hasEvent = true) : base(name, hasEvent)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            InputFormatting = inputFormatting;
            InputRegex = inputRegex;
            RegexGroupIndex = regexGroupIndex < 0 ? (int?)null : regexGroupIndex;
        }
    }
}
