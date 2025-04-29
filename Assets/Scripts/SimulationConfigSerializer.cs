using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityCore;
using ConstellationUI;
using Core.Json;
using System.Reflection;

/// <summary>
/// This class is responsible for collecting configs from several classes (ParticleController, MainVisualizer, etc.)
/// and generating a simulation config from them, as well as handling file saving. It also performs loading configs
/// from files.
/// </summary>
public class SimulationConfigSerializer : MonoBehaviour
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

    [SerializeField] private string[] _names;
    [SerializeField] private object[] _objects;

    private const string MetadataSectionName = "Metadata";

    private struct UpgradeRule
    {
        public Func<string, string> Converter;
        public string AppliesSinceVersion;
        public string UpgradesToVersion;

        /// <summary>
        /// Specify `AppliesSinceVersion` to set the lowest version that the upgrade should apply to <br/>
        /// Specify `UpgradesToVersion` to set the lowest version that the upgrade should NOT apply to
        /// </summary>
        /// <param name="converter">input: config file contents (usually json); output: new config file contents</param>
        public UpgradeRule(Func<string, string> converter, string appliesSinceVersion = null, string upgradeToVersion = null)
        {
            Converter = converter;
            AppliesSinceVersion = appliesSinceVersion;
            UpgradesToVersion = upgradeToVersion;
        }
    }

    private static string UpgradeSingleProperty(string section, string name, string configJson, Func<string, string> converter, 
        string newSection = null, string newName = null)
    {
        JsonTree fullTree = JsonSerializerUtility.ToJsonTree(configJson);
        string propertyValue = fullTree[section]?[name]?.ToJson();
        if (propertyValue is null) return configJson;

        string newValue = converter(propertyValue);
        newSection ??= section;
        newName ??= name;

        fullTree[section].Properties.Remove(name);
        if (fullTree[section].Properties.Count == 0) {
            fullTree.Properties.Remove(section);
        }

        if (!fullTree.Properties.ContainsKey(newSection))
            fullTree.Properties.Add(newSection, new JsonTree() { Properties = new Dictionary<string, JsonTree>() });

        fullTree[section].Properties.Add(newName, new JsonTree() { Value = newValue });
        return fullTree.ToJson();
    }

    // Older version had gradient keys in reverse order
    private static string InvertLineColorV2(string configJson) => 
        UpgradeSingleProperty("Visualizer", "LineColor", configJson, value => {
            Gradient val = DefaultJsonSerializer.Default.FromJson<Gradient>(value);
            val.alphaKeys = Utility.InvertGradientKeysInplace(val.alphaKeys);
            val.colorKeys = Utility.InvertGradientKeysInplace(val.colorKeys);
            return DefaultJsonSerializer.Default.ToJson(val);
        });

    // Older version had animation curve keys in reverse order
    private static string InvertAlphaCurveV2(string configJson) =>
        UpgradeSingleProperty("Visualizer", "AlphaCurve", configJson, value => {
            AnimationCurve val = DefaultJsonSerializer.Default.FromJson<AnimationCurve>(value);
            val.keys = Utility.InvertAnimationCurveKeysInplace(val.keys);
            return DefaultJsonSerializer.Default.ToJson(val);
        });

    // Removes `tangentMode` properties from Keyframes on AnimationCurve
    private static string StripDeprecatedAnimationCurveProperties(string configJson) =>
        UpgradeSingleProperty("Visualizer", "AlphaCurve", configJson, value => {
            JsonTree curveTree = JsonSerializerUtility.ToJsonTree(value);
            List<string> keys = JsonSerializerUtility.GetArrayElements(curveTree["keys"].Value);
            StringBuilder keysJson = new StringBuilder();
            JsonSerializerUtility.BeginArray(keysJson);

            for (int i = 0; i < keys.Count; i++)
            {
                JsonTree key = JsonSerializerUtility.ToJsonTree(keys[i]);
                key.Properties.Remove("tangentMode");
                keysJson.Append(key.ToJson());
                keysJson.Append(",");
            }
            JsonSerializerUtility.EndArray(keysJson);

            curveTree["keys"].Value = keysJson.ToString();
            return curveTree.ToJson();
        });

    /// <summary>
    /// Rules for converting older configs to newer ones
    /// </summary>
    private readonly List<UpgradeRule> UpgradeRules = new List<UpgradeRule>() {
        { new UpgradeRule(StripDeprecatedAnimationCurveProperties /* no harm in applying this to higher versions? */ ) },
        { new UpgradeRule(InvertLineColorV2, appliesSinceVersion: "1.0.0", upgradeToVersion: "1.1.16") },
        { new UpgradeRule(InvertAlphaCurveV2, appliesSinceVersion: "1.0.0", upgradeToVersion: "1.1.16") },
    };

    private static bool IsMatchingRule(string configVersion, UpgradeRule rule)
    {
        if (rule.AppliesSinceVersion is { } && Core.Algorithm.CompareVersions(configVersion, rule.AppliesSinceVersion) < 0) 
            return false;
        if (rule.UpgradesToVersion is { } && Core.Algorithm.CompareVersions(configVersion, rule.UpgradesToVersion) >= 0)
            return false;

        return true;
    }

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
    /// Opens file dialog to select config save location.
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
    /// Opens file dialog to select config load location.
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

    public string GetCurrentConfigJson(bool prettyPrint = true) => MultipleObjectsToJson(_names, _objects, prettyPrint);
    
    /// <summary>
    /// Serializes config in json and saves it to the specified file
    /// If the file already exists, it is overwritten
    /// </summary>
    /// <param name="path">Path to a file where config should be saved</param>
    public void SerializeConfig(string path)
    {
        string json = GetCurrentConfigJson();
        string parentPath = Path.GetDirectoryName(path);
        Directory.CreateDirectory(parentPath);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Reads specified file and deserializes config from its contents.
    /// The simulation gets restarted
    /// </summary>
    /// <param name="path">Path to a file to read config from</param>
    /// <returns>Non-negative number of loaded properties</returns>
    public int DeserializeConfigFromFile(string path)
    {
        string json = File.ReadAllText(path);
        return DeserializeJsonConfig(json);
    }

    public int DeserializeJsonConfig(string json)
    {
        int deserialized = MultipleObjectOverwriteFromJson(json, _names, _objects);
    
        // Restart the simulation
        _particleController.RestartSimulation();
        return deserialized;
    }

    public string MultipleObjectsToJson(string[] names, object[] objects, bool prettyPrint)
    {
        if (names.Length != objects.Length) throw new ArgumentException("Lengths of arguments do not match");

        StringBuilder json = new StringBuilder();

        JsonSerializerUtility.BeginObject(json);
        JsonSerializerUtility.SerializeDefault(json, MetadataSectionName, SimulationConfigMetadata.Default);
        for (int i = 0; i < names.Length; i++)
            JsonSerializerUtility.PrintProperty(json, names[i], ConfigJsonSerializer.ConfigToJson(objects[i], false));

        JsonSerializerUtility.EndObject(json);

        return prettyPrint ? JsonSerializerUtility.Prettify(json.ToString()) : json.ToString();
    }

    /// <summary>
    /// Apply necessary upgrades to convert from older config version, if applicable
    /// </summary>
    // And yes, this is the worst implementation performance-wise, feel free to hate it.
    private string UpgradeSimulationConfig(string json)
    {
        SimulationConfigMetadata meta = new SimulationConfigMetadata() { Version = "1.0.0" };
        string newConfig = json;

        if (JsonSerializerUtility.GetProperties(json).TryGetValue(MetadataSectionName, out string metaJson)) {
            ConfigJsonSerializer.OverwriteConfigFromJson(metaJson, meta);
            int[] version = Core.Algorithm.ParseVersion(meta.Version);
            if (version is null) {
                Debug.LogWarning($"Unknown version in simulation config: {meta.Version}");
                meta.Version = "1.0.0";
            }
            else if (Core.Algorithm.CompareVersions(version, Core.Algorithm.ParseVersion(Application.version)) > 0)
            {
                Debug.LogWarning($"Simulation config was created in a newer version of the app ({meta.Version})");
            }
        }

        for (int upgradeIndex = 0; upgradeIndex < UpgradeRules.Count; upgradeIndex++) {
            UpgradeRule rule = UpgradeRules[upgradeIndex];

            if (!IsMatchingRule(meta.Version, rule)) continue;

            newConfig = rule.Converter(newConfig);
        }

        JsonTree configTree = JsonSerializerUtility.ToJsonTree(newConfig);
        meta.Version = Application.version; // make sure now config is exactly current version
        configTree.Properties.Remove(MetadataSectionName);
        configTree.Add(MetadataSectionName, new JsonTree() { Value = ConfigJsonSerializer.ConfigToJson(meta) });

        return configTree.ToJson();
    }

    public int MultipleObjectOverwriteFromJson(string json, string[] names, object[] objects)
    {
        string upgradedJson;
        try {
            upgradedJson = UpgradeSimulationConfig(json);
        } catch (Exception e) {
            Debug.LogError("Upgrade error:");
            Debug.LogException(e);
            throw new Exception($"Failed to upgrade simulation config: {e.Message}");
        }
        Dictionary<string, string> sections = JsonSerializerUtility.GetProperties(upgradedJson);
        if (sections.ContainsKey(MetadataSectionName)) sections.Remove(MetadataSectionName);

        // Deserialize twice to correctly set min/max properties (TODO: fix)
        int deserialized = 0;
        for (int i = 0; i < 2; i++) {
            deserialized = 0;
            foreach (var keyValue in sections) {
                int index = Array.IndexOf(names, keyValue.Key);
                if (index < 0) {
                    Debug.Log($"Unknown system encountered in specified config: {keyValue.Key}");
                    continue;
                }
                deserialized += ConfigJsonSerializer.OverwriteConfigFromJson(keyValue.Value, objects[index]);
            }
        }

        return deserialized;
    }

    /// <summary>
    /// An OnDialogClose callback for a FileDialog, that is used by SaveConfig()
    /// If result if `false`, it just returns `true`, so that the dialog can be closed
    /// Otherwise, it tries to save config to the selected file, returning `true` on success
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
                _fileDialog.Manager.ShowMessageBox("Error", $"An unknown error occurred while saving config. Message: {e.Message}",
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
    /// Otherwise, it tries to load selected file as config, returning `true` on success
    /// or `false` on failure
    /// </summary>
    private bool TryLoadConfig(MonoDialog fileDialog, bool result)
    {
        if (result == false) return true;

        FileInfo fileInfo = new FileInfo(_fileDialog.FileName);
        int deserialized;

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
            deserialized = DeserializeConfigFromFile(fileInfo.FullName);
            if (deserialized == 0)
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
            _fileDialog.Manager.ShowMessageBox("Error", $"An unknown error occurred while parsing file. Message:\n<color=red>{ex.Message}</color>",
                StandardMessageBoxIcons.Error, _fileDialog);
            return false;
        }

        _fileDialog.Manager.ShowMessageBox("File loaded", $"Config has been loaded successfully. In total" +
            $" {deserialized} Constellation properties were loaded.", StandardMessageBoxIcons.Success, _fileDialog);
        return true;
    }
}