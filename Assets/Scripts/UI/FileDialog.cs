using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;

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
	[SerializeField] private Button _parentFolderButton;
	[SerializeField] private CustomDropdown _fileFilterDropdown;
	[SerializeField] private TMP_InputField _fileNameInputField;

	private List<GameObject> _fileObjects = new List<GameObject>();
	private DirectoryInfo _currentDirectory;
	private FileEntry _selected;
	private string _fileFilter;

	public string FileName
	{
		get => Path.Combine(_currentDirectory.FullName, _fileNameInputField.text);
		set => _fileNameInputField.text = value;
	}

	public bool DialogActive => gameObject.activeInHierarchy;

	protected override void Awake()
	{
		base.Awake();

		_currentDirectory = new DirectoryInfo(".");
		_parentFolderButton.onClick.AddListener(GoToParentDirectory);
		_fileFilterDropdown.onValueChanged.AddListener(OnFileFilterChanged);
		_fileFilterDropdown.value = 0;
		OnFileFilterChanged(_fileFilterDropdown.value);
		DialogOpened += x => UpdateFileView();
	}

	private void OnFileFilterChanged(int value)
	{
		if (value == 0)
			_fileFilter = "*.json";
		else
			_fileFilter = "*";

		if (DialogActive) UpdateFileView();
	}

	public void GoToParentDirectory()
	{
		_currentDirectory = _currentDirectory?.Parent;

		string customTitle = null;
		DirectoryInfo[] customDirectories = null;

		if (_currentDirectory == null)
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
		_currentDirectory = newDir;

		UpdateFileView();
	}

	private void OnFileClicked(FileEntry file)
	{
		if (_selected) _selected.Highlighted = false;
		_selected = file;
		_selected.Highlighted = true;
		_fileNameInputField.text = (file.Data as FileInfo).Name;
	}

	public void UpdateFileView(string customTitle = null, FileInfo[] customFiles = null, DirectoryInfo[] customDirectories = null)
	{
		DirectoryInfo[] directories;
		FileInfo[] files;

		try
		{
			directories = customDirectories ?? _currentDirectory?.GetDirectories();
			files = customFiles ?? _currentDirectory?.GetFiles(_fileFilter);
		}
		catch (System.Exception ex)
		{
			Manager.ShowMessageBox("Error", "Sorry, but an error occured while trying to display files." +
				$" The message is: {ex.Message}", StandardMessageBoxIcons.Error, this);
			return;
		}

		_pathInputField.text = customTitle ?? _currentDirectory.Name;

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

				Button button = newDirObject.GetComponentInChildren<Button>();
				button.onClick.AddListener(() => GoToDirectory(dir));
			}
		}

		if (files != null)
		{
			foreach (FileInfo file in files)
			{
				GameObject newFileObject = Instantiate(_filePrefab, _filesView);
				_fileObjects.Add(newFileObject);
				TextMeshProUGUI label = newFileObject.GetComponentInChildren<TextMeshProUGUI>();
				label.text = file.Name;

				FileEntry fileEntry = newFileObject.GetComponentInChildren<FileEntry>();
				fileEntry.Data = file;

				Button button = newFileObject.GetComponentInChildren<Button>();
				button.onClick.AddListener(() => OnFileClicked(fileEntry));
			}
		}
	}
}
