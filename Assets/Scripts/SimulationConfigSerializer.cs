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

    private string[] _names;
    private object[] _objects;

    private const string MetadataSectionName = "Metadata";

    private struct UpgradeRule
    {
        public Func<string, UpgradeResult> Converter;
        public string SectionName;
        public string JsonPropertyName;
        public string AppliesSinceVersion;
        public string ConvertsToVersion;

        /// <summary>
        /// convertsToVersion - when null, defaults to the current app version
        /// </summary>
        public UpgradeRule(Func<string, UpgradeResult> converter, string sectionName, string jsonPropertyName, 
                string appliesSinceVersion = "1.0.0", string upgradesToVersion = null)
        {
            Converter = converter;
            SectionName = sectionName;
            JsonPropertyName = jsonPropertyName;
            AppliesSinceVersion = appliesSinceVersion;
            ConvertsToVersion = upgradesToVersion ?? Application.version;
        }
    }

    private struct UpgradeResult {
        public string SectionName;
        public string JsonPropertyName;
        public string NewValue;

        public UpgradeResult(string newValue) {
            SectionName = null;
            JsonPropertyName = null;
            NewValue = newValue;
        }
    }

    private static UpgradeResult InvertLineColorV2(string propValue) {
        Gradient val = DefaultJsonSerializer.Default.FromJson<Gradient>(propValue);
        val.alphaKeys = Utility.InvertGradientKeysInplace(val.alphaKeys);
        val.colorKeys = Utility.InvertGradientKeysInplace(val.colorKeys);
        return new UpgradeResult(DefaultJsonSerializer.Default.ToJson(val));
    }

    private static UpgradeResult InvertAlphaCurveV2(string propValue) {
        AnimationCurve val = DefaultJsonSerializer.Default.FromJson<AnimationCurve>(propValue);
        val.keys = Utility.InvertAnimationCurveKeysInplace(val.keys);
        return new UpgradeResult(DefaultJsonSerializer.Default.ToJson(val));
    }

    /// <summary>
    /// Rules for converting older configs to newer ones
    /// </summary>
    private readonly List<UpgradeRule> UpgradeRules = new List<UpgradeRule>() {
        { new UpgradeRule(InvertLineColorV2, "Visualizer", "LineColor", upgradesToVersion: "1.1.16") },
        { new UpgradeRule(InvertAlphaCurveV2, "Visualizer", "AlphaCurve", upgradesToVersion: "1.1.16") },
    };

    // meta should NOT have higher version that ours! AND it should be valid too!
    private static bool IsMatchingRule(string sectionName, string propName, string propVersion, UpgradeRule rule)
    {
        if (sectionName != rule.SectionName || propName != rule.JsonPropertyName) return false;

        if (Core.Algorithm.CompareVersions(propVersion, rule.AppliesSinceVersion) < 0) return false;
        if (Core.Algorithm.CompareVersions(propVersion, rule.ConvertsToVersion) >= 0) return false;

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
    private string UpgradeSimulationConfig(string json)
    {
        JsonTree config = JsonSerializerUtility.ToJsonTree(json);
        JsonTree newConfig = null;
        SimulationConfigMetadata meta = new SimulationConfigMetadata() { Version = "1.0.0" };

        if (config.Properties.TryGetValue(MetadataSectionName, out JsonTree metaJsonTree)) {
            ConfigJsonSerializer.OverwriteConfigFromJson(metaJsonTree.ToJson(), meta);
            config.Properties.Remove(MetadataSectionName);
            int[] version = Core.Algorithm.ParseVersion(meta.Version);
            if (version is null) {
                Debug.LogWarning($"Unknown version in simulation config: {meta.Version}");
                meta.Version = "1.0.0";
            }
            else if (Core.Algorithm.CompareVersions(version, Core.Algorithm.ParseVersion(Application.version)) > 0) {
                Debug.LogWarning($"Simulation config was created in a newer version of the app ({meta.Version})");
                meta.Version = Application.version;
            }
        }

        Dictionary<(string, string), string> propVersionMap = new Dictionary<(string, string), string>();

        while (true) {
            int upgrades = 0;

            newConfig = new JsonTree() { Properties = new Dictionary<string, JsonTree>() };
            foreach (string sectionName in config.Properties.Keys) {
                JsonTree sectionTree = config.Properties[sectionName];
                foreach (string propName in sectionTree.Properties.Keys) {
                    string propVersion = GetPropertyVersion(sectionName, propName);
                    UpgradeRule? upgradeRule = null;

                    // we will be careful here and only apply a single matching rule at a time for a property
                    foreach (UpgradeRule rule in UpgradeRules)
                        if (IsMatchingRule(sectionName, propName, propVersion, rule)) 
                            upgradeRule = rule;

                    string propValue = sectionTree.Properties[propName].ToJson();
                    UpgradeResult upgradeResult = upgradeRule?.Converter(propValue) ?? new UpgradeResult(propValue);
                    upgradeResult.SectionName ??= sectionName;
                    upgradeResult.JsonPropertyName ??= propName;

                    if (!newConfig.Properties.ContainsKey(upgradeResult.SectionName))
                        newConfig.Add(upgradeResult.SectionName, new JsonTree());
                    JsonTree newSectionTree = newConfig.Properties[upgradeResult.SectionName];
                    newSectionTree.Add(upgradeResult.JsonPropertyName, new JsonTree() { Value = upgradeResult.NewValue });

                    if (upgradeRule.HasValue) {
                        upgrades++;
                        // in case section name or prop name change, remove the entry
                        if (propVersionMap.ContainsKey((sectionName, propName))) propVersionMap.Remove((sectionName, propName));
                        propVersionMap[(upgradeResult.SectionName, upgradeResult.JsonPropertyName)] = upgradeRule.Value.ConvertsToVersion;
                    }
                }
            }

            if (upgrades == 0) break;
            config = newConfig;
        }

        newConfig.Add(MetadataSectionName, new JsonTree() { Value = ConfigJsonSerializer.ConfigToJson(meta) });

        return newConfig.ToJson();

        string GetPropertyVersion(string sectionName, string propName) {
            if (propVersionMap.TryGetValue((sectionName, propName), out string version))
                return version;

            return meta.Version;
        }
    }

    public int MultipleObjectOverwriteFromJson(string json, string[] names, object[] objects)
    {
        string upgradedJson = UpgradeSimulationConfig(json);
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