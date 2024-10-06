using Core.Json;
using UnityEngine;

namespace ConstellationUI
{
    public class BenchmarkConfigFileDialogPlugin : FileDialogPlugin
    {
        [SerializeField] private TMPro.TextMeshProUGUI _titleLabel;
        [SerializeField] private TMPro.TextMeshProUGUI _descriptionLabel;
        [SerializeField] private Toggle _showAllFilesToggle;

        private FileDialog _parent;

        public override bool Enabled => _parent is { };

        private void Awake()
        {
            _showAllFilesToggle.IsCheckedChanged += x => _parent?.UpdateFileView();
        }

        public override void Enable(FileDialog parent)
        {
            if (_parent == parent) return;
            if (_parent is { }) Disable(_parent);
            _parent = parent;
            gameObject.SetActive(true);

            _parent.SelectedFileChanged += OnSelectedFileChanged;
            OnSelectedFileChanged(null);
        }

        public override void Disable(FileDialog parent)
        {
            if (_parent is null || _parent != parent) return;
            _parent.SelectedFileChanged -= OnSelectedFileChanged;
            _parent = null;
            gameObject.SetActive(false);
        }

        public override bool FilterFile(string path) => _showAllFilesToggle.IsChecked || IsBenchmarkConfig(path);

        private bool IsBenchmarkConfig(string path)
        {
            string contents = System.IO.File.ReadAllText(path);
            try
            {
                return DefaultJsonSerializer.Default.ReadJsonProperty(contents, nameof(BenchmarkConfig.BenchmarkVersion), out string _);
            }
            catch (JsonSerializerException) { }

            return false;
        }

        private void OnSelectedFileChanged(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _titleLabel.text = "";
                _descriptionLabel.text = "Select the benchmark config file";
                return;
            }
            else if (!IsBenchmarkConfig(path))
            {
                _titleLabel.text = "<color=red>Invalid file</color>";
                _descriptionLabel.text = "Selected file is not recognized as a benchmark config. Ensure the file contains `BenchmarkVersion` property";
                return;
            }

            try
            {
                BenchmarkConfig config = BenchmarkConfig.FromFile(path);
                _titleLabel.text = config.Name;
                _descriptionLabel.text = config.Description;
            }
            catch (System.Exception e)
            {
                _titleLabel.text = "<color=red>Error</color>";
                _descriptionLabel.text = $"<color=red>{e.Message}</color>";
            } 
        }
    }
}