namespace Core
{
    public class ModuleDescriptor
    {
        public string Name { get; set; }
        public object ModuleData { get; set; }
        public bool Enabled { get; set; }
        public bool Locked { get; set; }
        public bool HasProperties { get; set; }
    }
}
