namespace ConfigSerialization
{
    public class ListViewProperty : ConfigProperty
    {
        public object[] DisplayedOptions { get; }
        public string[] OptionNames { get; }
        public string SourcePropertyName { get; }

        public ListViewProperty(object[] displayedOptions = null, string[] optionNames = null, string name = null, 
            bool hasEvent = true) : base(name, hasEvent)
        {
            if (displayedOptions != null && optionNames != null && displayedOptions.Length != optionNames.Length)
                throw new System.ArgumentException("Lengths of displayed options and option names are not the same");

            DisplayedOptions = displayedOptions;
            OptionNames = optionNames;
        }

        public ListViewProperty(string sourcePropertyName, string name = null, bool hasEvent = true) : base(name, hasEvent)
        {
            SourcePropertyName = sourcePropertyName;
        }
    }

    public class DropdownProperty : ListViewProperty
    {
        public DropdownProperty(object[] displayedOptions = null, string[] optionNames = null, string name = null,
            bool hasEvent = true) : base(displayedOptions, optionNames, name, hasEvent) { }

        public DropdownProperty(string sourcePropertyName, string name = null, bool hasEvent = true) : base(sourcePropertyName, name, hasEvent) { }
    }
}
