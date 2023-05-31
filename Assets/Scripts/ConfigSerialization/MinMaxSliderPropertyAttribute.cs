namespace ConfigSerialization
{
	public class MinMaxSliderProperty : SliderProperty
	{
		public float MinMaxSpacing { get; }
		public string LowerLabel { get; }
		public string HigherLabel { get; }
		public string HigherPropertyName { get; }

		public MinMaxSliderProperty(float minSliderValue = 0, float maxSliderValue = 1, float minValue = float.MinValue, 
							float maxValue = float.MaxValue, string inputFormatting = null, string inputRegex = null, int regexGroupIndex = -1, 
							string name = null, string lowerLabel = null, string higherLabel = null, string higherPropertyName = null, 
							float minMaxSpacing = 0, bool hasEvent = true) :
			base(minSliderValue, maxSliderValue, minValue, maxValue, inputFormatting, inputRegex, regexGroupIndex, name, hasEvent)
		{
			LowerLabel = lowerLabel;
			HigherLabel = higherLabel;
			HigherPropertyName = higherPropertyName;
			MinMaxSpacing = minMaxSpacing;
		}
	}
}
