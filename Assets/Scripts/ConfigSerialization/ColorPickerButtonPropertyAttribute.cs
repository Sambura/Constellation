namespace ConfigSerialization
{
	public class ColorPickerButtonProperty : ConfigProperty
	{
		public bool UseAlpha { get; }
		public string DialogTitle { get; }

		public ColorPickerButtonProperty(bool useAlpha = true, string dialogTitle = null, string name = null, bool hasEvent = true)
			: base(name, hasEvent)
		{
			UseAlpha = useAlpha;
			DialogTitle = dialogTitle;
		}
	}
}
