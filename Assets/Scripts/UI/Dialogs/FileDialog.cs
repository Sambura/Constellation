using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using TMPro;
using System;

namespace ConstellationUI
{
    /// <summary>
    /// Class representing file choosing dialog - either for saving or for loading a file
    /// File dialog has a title, a list view of files and folders, file name filter selector,
    /// Input field for file name, input field for path, `Ok` and `Cancel` buttons
    /// </summary>
    public class FileDialog : MonoDialog
    {
        [SerializeField] private TMP_InputField _pathInputField;
        [SerializeField] private GameObject _filePrefab;
        [SerializeField] private GameObject _folderPrefab;
        [SerializeField] private RectTransform _filesView;
        [SerializeField] private UnityEngine.UI.Button _parentFolderButton;
        [SerializeField] private CustomDropdown _fileFilterDropdown;
        [SerializeField] private TMP_InputField _fileNameInputField;
        [SerializeField] private List<FileFilter> _fileFilters;

        [SerializeField] private GameObject _pluginContainer;
        [SerializeField] private UIArranger _uiArranger;
        [SerializeField] private List<FileDialogPlugin> _plugins;

        [System.Serializable] public struct FileFilter
        {
            public string Description;
            public string Pattern;
        }

        private List<GameObject> _fileObjects = new List<GameObject>();
        private DirectoryInfo _currentDirectory;
        private ListEntry _selected;
        private string _fileFilter;
        private Dictionary<object, DirectoryInfo> _boundCallersDirectories = new Dictionary<object, DirectoryInfo>();
        private object _currentCaller;

        public string FileName
        {
            get => Path.Combine(_currentDirectory.FullName, _fileNameInputField.text);
            set => _fileNameInputField.text = value;
        }

        public List<FileFilter> FileFilters
        {
            get => _fileFilters;
            set
            {
                _fileFilters = value;
                _fileFilter = value.Count > 0 ? _fileFilters[0].Pattern : "*";
                UpdateFileFiltersDropdown();
            }
        }

        public DirectoryInfo CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                _currentDirectory = value;
                if (_currentCaller != null) _boundCallersDirectories[_currentCaller] = value;
                if (DialogActive) UpdateFileView();
            }
        }

        public event Action<string> SelectedFileChanged;

        protected override void Awake()
        {
            base.Awake();

            _currentDirectory = new DirectoryInfo(".");
            UpdateFileFiltersDropdown();
            _parentFolderButton.onClick.AddListener(GoToParentDirectory);
            _fileFilterDropdown.onValueChanged.AddListener(OnFileFilterChanged);
            _fileFilterDropdown.value = 0;
            OnFileFilterChanged(_fileFilterDropdown.value);
            DialogOpened += x => _currentCaller = null;
            DialogOpened += x => UpdateFileView();
            DialogOpened += x => DisablePlugins();
        }

        private void UpdateFileFiltersDropdown()
        {
            _fileFilterDropdown.ClearOptions();

            List<string> options = new List<string>();
            foreach (FileFilter filter in _fileFilters)
                options.Add($"{filter.Description} \"{filter.Pattern}\"");

            _fileFilterDropdown.AddOptions(options);
        }

        private void OnFileFilterChanged(int value)
        {
            _fileFilter = _fileFilters[value].Pattern;

            if (DialogActive) UpdateFileView();
        }

        public void GoToParentDirectory()
        {
            CurrentDirectory = _currentDirectory?.Parent;

            string customTitle = null;
            DirectoryInfo[] customDirectories = null;

            if (CurrentDirectory is null)
            {
                string[] drives = Directory.GetLogicalDrives();
                customDirectories = new DirectoryInfo[drives.Length];
                for (int i = 0; i < drives.Length; i++)
                    customDirectories[i] = new DirectoryInfo(drives[i]);

                customTitle = "Drives";
            }

            UpdateFileView(customTitle, null, customDirectories);
        }

        private void GoToDirectory(DirectoryInfo newDir)
        {
            CurrentDirectory = newDir;

            UpdateFileView();
        }

        private void OnFileClicked(ListEntry file)
        {
            if (_selected) _selected.Highlighted = false;
            _selected = file;
            _selected.Highlighted = true;
            _fileNameInputField.text = (file.Data as FileInfo).Name;
            SelectedFileChanged?.Invoke(FileName);
        }

        private static FileInfo[] GetFilesByPattern(DirectoryInfo dir, string pattern)
        {
            if (dir is null) return null;

            string[] filters = pattern.Split('|');
            List<FileInfo> files = new List<FileInfo>();

            foreach (string filter in filters) files.AddRange(dir.GetFiles(filter));

            return files.ToArray();
        }

        public void UpdateFileView(string customTitle = null, FileInfo[] customFiles = null, DirectoryInfo[] customDirectories = null)
        {
            DirectoryInfo[] directories;
            FileInfo[] files;

            try
            {
                directories = customDirectories ?? CurrentDirectory?.GetDirectories();
                files = customFiles ?? GetFilesByPattern(CurrentDirectory, _fileFilter);
            }
            catch (System.Exception ex)
            {
                Manager.ShowMessageBox("Error", "Sorry, but an error occurred while trying to display files. " +
                    $"The message is:\n<color=red>{ex.Message}</color>", StandardMessageBoxIcons.Error, this);
                return;
            }

            _pathInputField.text = customTitle ?? CurrentDirectory?.Name ?? "<error>";

            foreach (GameObject file in _fileObjects) Destroy(file);
            _fileObjects.Clear();
            _selected = null;

            if (directories != null)
            {
                foreach (DirectoryInfo dir in directories)
                {
                    GameObject newDirObject = Instantiate(_folderPrefab, _filesView);
                    _fileObjects.Add(newDirObject);
                    TextMeshProUGUI label = newDirObject.GetComponentInChildren<TextMeshProUGUI>();
                    label.text = dir.Name;

                    var button = newDirObject.GetComponentInChildren<UnityEngine.UI.Button>();
                    button.onClick.AddListener(() => GoToDirectory(dir));
                }
            }

            if (files != null)
            {
                foreach (FileInfo file in files)
                {
                    if (!CheckAgainstPlugins(file)) continue;

                    GameObject newFileObject = Instantiate(_filePrefab, _filesView);
                    _fileObjects.Add(newFileObject);
                    TextMeshProUGUI label = newFileObject.GetComponentInChildren<TextMeshProUGUI>();
                    label.text = file.Name;

                    ListEntry fileEntry = newFileObject.GetComponentInChildren<ListEntry>();
                    fileEntry.Data = file;

                    var button = newFileObject.GetComponentInChildren<UnityEngine.UI.Button>();
                    button.onClick.AddListener(() => OnFileClicked(fileEntry));
                }
            }
        }

        private bool CheckAgainstPlugins(FileInfo file)
        {
            foreach (var plugin in _plugins)
            {
                if (plugin.Enabled && !plugin.FilterFile(file.FullName)) return false;
            }

            return true;
        }

        /// <summary>
        /// Binds/syncs the current directory for the given caller object. Call this function after the ShowDialog() function,
        /// to make file dialog switch to the last directory that was opened with this caller object. If no directory was 
        /// remembered, file dialog does not switch the current directory
        /// </summary>
        public void SyncCurrentDirectory(object @object)
        {
            _currentCaller = @object;

            if (_boundCallersDirectories.TryGetValue(@object, out DirectoryInfo directoryInfo))
            {
                CurrentDirectory = directoryInfo;
                return;
            }

            _boundCallersDirectories.Add(@object, CurrentDirectory);
        }

        public void EnablePlugins(params System.Type[] pluginTypes)
        {
            foreach (var plugin in _plugins) 
                plugin.SetEnable(this, pluginTypes.Contains(plugin.GetType()));

            bool anyEnabled = pluginTypes.Length > 0;
            _uiArranger.SelectedConfigurationName = anyEnabled ? "Extended" : "Default";
            _pluginContainer.SetActive(anyEnabled);
            UpdateFileView();
        }

        public void DisablePlugins() => EnablePlugins();
    }
}