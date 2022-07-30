using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class FileDialog : MonoDialog
{
	[SerializeField] private TMP_InputField _pathInputField;
	[SerializeField] private TextMeshProUGUI _titleLabel;
	[SerializeField] private GameObject _filePrefab;
	[SerializeField] private GameObject _folderPrefab;
	[SerializeField] private RectTransform _filesView;
	[SerializeField] private Button _parentFolderButton;
	[SerializeField] private GameObject _errorScreen;
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

	public bool DialogOpened => gameObject.activeInHierarchy;

	public System.Action<string> OnOkClicked;

	protected override void Awake()
	{
		base.Awake();

		_currentDirectory = new DirectoryInfo(Application.persistentDataPath);
		_errorScreen.SetActive(false);
		_parentFolderButton.onClick.AddListener(GoToParentDirectory);
		_fileFilterDropdown.onValueChanged.AddListener(OnFileFilterChanged);
		_fileFilterDropdown.value = 0;
		OnFileFilterChanged(_fileFilterDropdown.value);
		//ShowDialog("Test this shit :)");
	}

	private void OnFileFilterChanged(int value)
	{
		if (value == 0)
			_fileFilter = "*.json";
		else
			_fileFilter = "*";

		if (DialogOpened) UpdateFileView();
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

	public void ShowDialog(string title)
	{
		gameObject.SetActive(true);

		_titleLabel.text = title;
		UpdateFileView();
	}

	protected override void OnOkButtonPressed()
	{
		base.OnOkButtonPressed();
		OnOkClicked?.Invoke(FileName);
		OnOkClicked = null;
	}

	public void UpdateFileView(string customTitle = null, FileInfo[] customFiles = null, DirectoryInfo[] customDirectories = null)
	{
		_pathInputField.text = customTitle ?? _currentDirectory.Name;

		foreach (GameObject file in _fileObjects) Destroy(file);
		_fileObjects.Clear();
		_selected = null;

		DirectoryInfo[] directories;
		FileInfo[] files;

		try
		{
			directories = customDirectories ?? _currentDirectory?.GetDirectories();
			files = customFiles ?? _currentDirectory?.GetFiles(_fileFilter);
		}
		catch (System.Exception ex)
		{
			_errorScreen.SetActive(true);
			Debug.Log(ex.Message);
			Debug.LogError(ex.StackTrace);
			return;
		}

		_errorScreen.SetActive(false);

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
