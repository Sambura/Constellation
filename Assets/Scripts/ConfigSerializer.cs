using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityCore;

public class ConfigSerializer : MonoBehaviour
{
    [SerializeField] private List<SerializablePair> _toSerialize;
	[SerializeField] private FileDialog _fileDialog;
	[SerializeField] private ParticleController _particleController;
    //[SerializeField] private string _targetPath = "Json/config.json";

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

    public void SaveConfig()
    {
        _fileDialog.FileName = "config.json";
        _fileDialog.ShowDialog("Select save location");
        _fileDialog.OnOkClicked = (x) => SerializeConfig((x as FileDialog).FileName);
    }

    public void SerializeConfig(string path)
    {
        string json = MultipleObjectsToJson(_names, _objects, true);
        string parentPath = Path.GetDirectoryName(path);
        Directory.CreateDirectory(parentPath);
        File.WriteAllText(path, json);
	}

    public void LoadConfig()
	{
        _fileDialog.ShowDialog("Select config to load");
        _fileDialog.OnOkClicked = (x) => DeserializeConfig((x as FileDialog).FileName);
    }

    public void DeserializeConfig(string path)
    {
        string json = File.ReadAllText(path);
        // Two times to ensure min/max properties deserialized correctly
        MultipleObjectOverwriteFromJson(json, _names, _objects);
        MultipleObjectOverwriteFromJson(json, _names, _objects);

        _particleController.ReinitializeParticles();
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

    public void MultipleObjectOverwriteFromJson(string json, string[] names, object[] objects)
	{
        Dictionary<string, string> jsons = JsonSerializerUtility.GetProperties(json);

        foreach (var keyValue in jsons)
        {
            int index = Array.IndexOf(names, keyValue.Key);
            if (index < 0) continue;
            ConfigJsonSerializer.OverwriteConfigFromJson(keyValue.Value, objects[index]);
        }
    }
}