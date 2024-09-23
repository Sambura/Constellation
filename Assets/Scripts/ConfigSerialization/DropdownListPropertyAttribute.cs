namespace ConfigSerialization
{
    public class DropdownListProperty : ConfigProperty
    {
        public object[] DisplayedOptions { get; }
        public string[] OptionNames { get; }

        public DropdownListProperty(object[] displayedOptions = null, string[] optionNames = null, string name = null, 
            bool hasEvent = true) : base(name, hasEvent)
        {
            if (displayedOptions != null && optionNames != null && displayedOptions.Length != optionNames.Length)
                throw new System.ArgumentException("Lengths of displayed options and option names are not the same");

            DisplayedOptions = displayedOptions;
            OptionNames = optionNames;
        }
    }
}
