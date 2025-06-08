using System.Collections.Generic;

namespace Core
{
    public class ModuleDescriptor
    {
        public string Name { get; set; }
        public object ModuleData { get; set; }
        public bool Enabled { get; set; }
        public bool Locked { get; set; }
        public bool HasProperties { get; set; }
        public List<int> QuickToggleStates { get; set; }

        public virtual List<(string icon, int stateCount, object data)> GetQuickToggles() {
            return new();
        }
    }
}
