namespace ConfigSerialization
{
    public class GradientPickerButtonProperty : ConfigProperty
    {
        public string DialogTitle { get; }

        public GradientPickerButtonProperty(string dialogTitle = null, string name = null, bool hasEvent = true) : base(name, hasEvent)
        {
            DialogTitle = dialogTitle;
        }
    }
}
