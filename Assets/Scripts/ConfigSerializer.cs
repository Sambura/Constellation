using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityCore;
using ConstellationUI;

public class ConfigSerializer : MonoBehaviour
{
    [SerializeField] private List<SerializablePair> _toSerialize;
	[SerializeField] private FileDialog _fileDialog;
	[SerializeField] private ParticleController _particleController;
    [SerializeField] private long _loadFileSizeLimit = 1024 * 1024 * 10; // 10 MB

	[Serializable] private class SerializablePair
	{
        public string Name;
        public MonoBehaviour Object;
	}

    private string[] _names;
    private object[] _objects;

	private void Start()
	{
        _names = new string[_toSerialize.Count];
        _objects = new object[_toSerialize.Count];

        for (int i = 0; i < _toSerialize.Count; i++)
		{
            _names[i] = _toSerialize[i].Name;
            _objects[i] = _toSerialize[i].Object;
		}
    }

    /// <summary>
    /// Openes file dialog to select config save location.
    /// When the file dialog is closed, an attempt to serialize config to disk is made
    /// </summary>
    public void SaveConfig()
    {
        SetFileFilters();
        _fileDialog.FileName = "config.json";
        _fileDialog.ShowDialog("Select save location", TrySaveConfig);
        _fileDialog.SyncCurrentDirectory(this);
    }

    /// <summary>
    /// Openes file dialog to select config load location.
    /// When the file dialog is closed, an attempt to deserialize config from disk is made
    /// </summary>
    public void LoadConfig()
	{
        SetFileFilters();
        _fileDialog.ShowDialog("Select config to load", TryLoadConfig);
        _fileDialog.SyncCurrentDirectory(this);
    }

    private void SetFileFilters()
	{
        _fileDialog.FileFilters = new List<FileDialog.FileFilter>()
        {
            new FileDialog.FileFilter() { Description = "Json files", Pattern = "*.json"},
            new FileDialog.FileFilter() { Description = "All files", Pattern = "*"}
        };
	}

    /// <summary>
    /// Serializes config in json and saves it to the specified file
    /// If the file already exists, it is overwritten
    /// </summary>
    /// <param name="path">Path to a file where config should be saved</param>
    public void SerializeConfig(string path)
    {
        string json = MultipleObjectsToJson(_names, _objects, true);
        string parentPath = Path.GetDirectoryName(path);
        Directory.CreateDirectory(parentPath);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Reads specified file and deserializes config from its contents.
    /// The simulation gets restarted
    /// </summary>
    /// <param name="path">Path to a file to read config from</param>
    public int DeserializeConfig(string path)
    {
        string json = File.ReadAllText(path);
        int deserealized = 0;

        // Two times to ensure min/max properties deserialized correctly
        MultipleObjectOverwriteFromJson(json, _names, _objects);
        deserealized = MultipleObjectOverwriteFromJson(json, _names, _objects);

        // Restard the simulation
        _particleController.RestartSimulation();
        return deserealized;
    }

    public string MultipleObjectsToJson(string[] names, object[] objects, bool prettyPrint)
	{
        if (names.Length != objects.Length) throw new ArgumentException("Lengths of arguments do not match");
        int length = 0;
		string[] jsons = new string[names.Length];
        for (int i = 0; i < names.Length; i++)
		{
			jsons[i] = ConfigJsonSerializer.ConfigToJson(objects[i], false);
            length += jsons[i].Length;
            length += names[i].Length;
		}

        // This length will only work for non-pretty print
        StringBuilder json = new StringBuilder(length + names.Length * 4 + 2);

        json.Append('{');
        for (int i = 0; i < jsons.Length; i++)
		{
            JsonSerializerUtility.PrintPropertyName(json, names[i]);
            json.Append(jsons[i]);
            if (i != jsons.Length - 1) json.Append(',');
		}
        json.Append('}');

        return JsonSerializerUtility.Prettyfy(json.ToString(), prettyPrint);
	}

    public int MultipleObjectOverwriteFromJson(string json, string[] names, object[] objects)
	{
        Dictionary<string, string> jsons = JsonSerializerUtility.GetProperties(json);
        int deserealized = 0;

        foreach (var keyValue in jsons)
        {
            int index = Array.IndexOf(names, keyValue.Key);
            if (index < 0) continue;
            deserealized += ConfigJsonSerializer.OverwriteConfigFromJson(keyValue.Value, objects[index]);
        }

        return deserealized;
    }

    /// <summary>
    /// An OnDialogClose callback for a FileDialog, that is used by SaveConfig()
    /// If result if `false`, it just returns `true`, so that the dialog can be closed
    /// Otherwise, it tries to save config to the selected file, returnting `true` on success
    /// or `false` on failure
    /// </summary>
    private bool TrySaveConfig(MonoDialog fileDialog, bool result)
	{
        if (result == false) return true;

        string fileName = _fileDialog.FileName; // make a copy of the string

        if (File.Exists(fileName))
		{
            _fileDialog.Manager.ShowOkCancelMessageBox("Confirmation", "The file with the given name already exists." +
               " Do you want to replace it?", StandardMessageBoxIcons.Question, x => { if (x) return DoSaveConfig(fileName); return true; }, _fileDialog);

            return false;
        }

        DoSaveConfig(fileName);
        return true;

        bool DoSaveConfig(string path)
        {
            try
            {
                SerializeConfig(path);
                _fileDialog.Manager.ShowMessageBox("File saved", $"Config saved successfully to the file `{fileName}`",
                    StandardMessageBoxIcons.Success, _fileDialog);
            }
			catch (Exception e)
			{
                _fileDialog.Manager.ShowMessageBox("Error", $"An unknown error occured while saving config. Message: {e.Message}",
                    StandardMessageBoxIcons.Error, _fileDialog);
            }
            fileDialog.OnDialogClosing = null;
            fileDialog.CloseDialog(true);
            return false;
        }
    }

    /// <summary>
    /// An OnDialogClose callback for a FileDialog, that is used by LoadConfig()
    /// If result if `false`, it just returns `true`, so that the dialog can be closed
    /// Otherwise, it tries to load selected file as config, returnting `true` on success
    /// or `false` on failure
    /// </summary>
    private bool TryLoadConfig(MonoDialog fileDialog, bool result)
    {
        if (result == false) return true;

        FileInfo fileInfo = new FileInfo(_fileDialog.FileName);
        int deserealized;

        if (fileInfo.Exists == false)
        {
            _fileDialog.Manager.ShowMessageBox("Failure", "Selected file could not be loaded, since it was not found.",
                StandardMessageBoxIcons.Error, _fileDialog);
            return false; // Do not close dialog, since the invalid file was selected
        }

        if (fileInfo.Length > _loadFileSizeLimit)
        {
            _fileDialog.Manager.ShowMessageBox("Warning", "Selected file was not loaded, since it is too large. Please" +
                " make sure that you selected the correct file.", StandardMessageBoxIcons.Warning, _fileDialog);
            return false;
        }

        try
        {
            deserealized = DeserializeConfig(fileInfo.FullName);
            if (deserealized == 0)
			{
                _fileDialog.Manager.ShowMessageBox("Warning", "No Constellation properties were found in the " + 
                    "loaded file. You have probably selected wrong file to load.", StandardMessageBoxIcons.Warning, _fileDialog);
                return true;
            }
        }
        catch (JsonSerializerException e)
        {
            _fileDialog.Manager.ShowMessageBox("Error", "Failed to parse json. " +
                $"Message:\n<color=red>{e.Message}</color>", StandardMessageBoxIcons.Error, _fileDialog);
            return false;
        }
        catch (Exception ex)
        {
            _fileDialog.Manager.ShowMessageBox("Error", $"An unknown error occured while parsing file. Message:\n<color=red>{ex.Message}</color>",
                StandardMessageBoxIcons.Error, _fileDialog);
            return false;
        }

        _fileDialog.Manager.ShowMessageBox("File loaded", $"Config has been loaded successfully. In total" +
			$" {deserealized} Constellation properties were loaded.", StandardMessageBoxIcons.Success, _fileDialog);
        return true;
    }
}