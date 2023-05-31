namespace ConfigSerialization
{
	public class SliderProperty : ConfigProperty
	{
		public float MinValue { get; }
		public float MaxValue { get; }
		public float MinSliderValue { get; }
		public float MaxSliderValue { get; }
		public string InputFormatting { get; }
		public string InputRegex { get; }
		public int? RegexGroupIndex { get; }

		public SliderProperty(float minSliderValue = 0, float maxSliderValue = 1, float minValue = float.MinValue, float maxValue = float.MaxValue,
							string inputFormatting = null, string inputRegex = null, int regexGroupIndex = -1, string name = null, bool hasEvent = true)
				: base(name, hasEvent)
		{
			MinValue = minValue;
			MaxValue = maxValue;
			MinSliderValue = minSliderValue;
			MaxSliderValue = maxSliderValue;

			if (MaxSliderValue > MaxValue) throw new System.ArgumentException("MaxSiderValue cannot be larger than MaxValue");
			if (MinSliderValue < MinValue) throw new System.ArgumentException("MinSiderValue cannot be smaller than MinValue");

			InputFormatting = inputFormatting;
			InputRegex = inputRegex;
			RegexGroupIndex = regexGroupIndex < 0 ? (int?)null : regexGroupIndex;
		}
	}
}
