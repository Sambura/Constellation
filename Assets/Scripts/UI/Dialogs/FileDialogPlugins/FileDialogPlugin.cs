using UnityEngine;

namespace ConstellationUI
{
    /// <summary>
    /// Base class for plugins that are used with <see cref="FileDialog"/>. Plugins can have additional 
    /// UI controls in the file dialog, as well as can alter the behavior of the file dialog. Also provide
    /// a possibility for advanced file filtering in the file dialog
    /// </summary>
    public abstract class FileDialogPlugin : MonoBehaviour
    {
        /// <summary>
        /// Is plugin enabled right now?
        /// </summary>
        public abstract bool Enabled { get; }

        /// <summary>
        /// Enable the plugin
        /// </summary
        public abstract void Enable(FileDialog parent);
        /// <summary>
        /// Disable the plugin
        /// </summary>
        public abstract void Disable(FileDialog parent);
        /// <summary>
        /// Convenience method for calling Enable() or Disable()
        /// </summary>
        public void SetEnable(FileDialog parent, bool enabled) { if (enabled) Enable(parent); else Disable(parent); }
        /// <summary>
        /// Called by the parent FileDialog to filter displayed files
        /// </summary>
        public abstract bool FilterFile(string path);
    }
}
