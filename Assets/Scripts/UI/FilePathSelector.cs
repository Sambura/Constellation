using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace ConstellationUI
{
    public class FilePathSelector : LabeledUIElement
    {
        [Header("Objects")]
        [SerializeField] private TextMeshProUGUI _fileNameLabel;
        [SerializeField] private Button _browseButton;
        [SerializeField] private FileDialog _fileDialog;

        [Header("Parameters")]
        [SerializeField] private string _dialogTitle = "Select file";
        [SerializeField] private FileDialog.FileFilter[] _fileFilters = { new FileDialog.FileFilter() { Description = "All files", Pattern = "*"} };
        [SerializeField] private bool _checkFileExists = false;
        [SerializeField] private bool _findFileDialog = true;

        private string _selectedPath;
        private Func<string, string> _fileNameDisplayedConverter;

        public FileDialog FileDialog
        {
            get => _fileDialog != null ? _fileDialog : (_findFileDialog ? _fileDialog = FindObjectOfType<FileDialog>(true) : null);
            /* Set can be added as needed, but proper support for dynamic property set may be a pain to implement */
        }

        public Func<string, string> FileNameDisplayedConverter
        {
            get => _fileNameDisplayedConverter;
            set
            {
                _fileNameDisplayedConverter = value;
                DisplaySelectedPath();
            }
        }

        public string SelectedPath {
            get => _selectedPath;
            set
            {
                if (value == _selectedPath) return;
                _selectedPath = value;
                SelectedPathChanged?.Invoke(value);
                DisplaySelectedPath();
            }
        }

        public FileDialog.FileFilter[] FileFilters
        {
            get => _fileFilters;
            set => _fileFilters = value;
        }

        public bool CheckFileExists
        {
            get => _checkFileExists;
            set
            {
                if (value == _checkFileExists) return;
                _checkFileExists = value;
                if (value && SelectedPath is { } && !File.Exists(SelectedPath))
                    SelectedPath = null;
            }
        }

        public string DialogTitle
        {
            get => _dialogTitle;
            set => _dialogTitle = value;
        }

        public event Action<string> SelectedPathChanged;

        private void DisplaySelectedPath()
        {
            _fileNameLabel.alpha = SelectedPath == null ? 0.5f : 1;

            string text = FileNameDisplayedConverter?.Invoke(SelectedPath) ?? SelectedPath ?? "Not specified";

            _fileNameLabel.text = text;
        }

        private bool FileDialogCallback(MonoDialog fileDialog, bool result)
        {
            if (!result) return true;

            string fileName = ((FileDialog)fileDialog).FileName;

            if (_checkFileExists && !File.Exists(fileName))
            {
                fileDialog.Manager.ShowMessageBox("Error", "The specified file does not exist",
                    StandardMessageBoxIcons.Error, fileDialog);
                return false;
            }

            SelectedPath = fileName;
            return true;
        }

        private void OnBrowseButtonClick()
        {
            FileDialog.FileFilters = new List<FileDialog.FileFilter>(_fileFilters);
            FileDialog.ShowDialog(_dialogTitle, FileDialogCallback);
            FileDialog.SyncCurrentDirectory(this);
        }

        private void Start()
        {
            _browseButton.Click += OnBrowseButtonClick;
        }

        private void OnDestroy()
        {
            _browseButton.Click -= OnBrowseButtonClick;
        }
    }
}