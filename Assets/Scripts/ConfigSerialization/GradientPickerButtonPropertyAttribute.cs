namespace ConfigSerialization
{
    public class CurvePickerButtonProperty : ConfigProperty
    {
        public string DialogTitle { get; }

        public CurvePickerButtonProperty(string dialogTitle = null, string name = null, bool hasEvent = true) : base(name, hasEvent)
        {
            DialogTitle = dialogTitle;
        }
    }
}
