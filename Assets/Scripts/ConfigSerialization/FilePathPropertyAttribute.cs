using ConstellationUI;
using System;
using Core;

namespace ConfigSerialization
{
    public class FilePathProperty : ConfigProperty
    {
        public string DialogTitle { get; set; }
        public bool CheckFileExists { get; set; }
        public FileDialog.FileFilter[] Filters { get; set; }
        public Func<string, string> StringConverter { get; set; } = null;

        public FilePathProperty(string dialogTitle = null, bool checkFileExists = false, string[] filters = null,
            Type displayedTextConverter = null, string name = null, bool hasEvent = true) : base(name, hasEvent)
        {
            DialogTitle = dialogTitle;
            CheckFileExists = checkFileExists;

            filters ??= new string[] { "All files", "*" };
            if (filters.Length % 2 == 1) throw new ArgumentException("`filters` parameter should have even number of elements");
            Filters = new FileDialog.FileFilter[filters.Length / 2];

            for (int i = 0; i < filters.Length; i += 2) {
                Filters[i / 2] = new FileDialog.FileFilter() { Description = filters[i], Pattern = filters[i + 1] };
            }

            if (displayedTextConverter is null) return;
            if (!(Activator.CreateInstance(displayedTextConverter) is IStringTransformer transformer)) 
                throw new ArgumentException("Invalid string converter type provided");
            StringConverter = new Func<string, string>(transformer.Transform);
        }
    }
}
