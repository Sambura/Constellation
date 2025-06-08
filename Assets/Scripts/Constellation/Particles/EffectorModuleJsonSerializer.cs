using System.Collections.Generic;
using System.Text;
using System;
using Core.Json;

public class EffectorModuleJsonSerializer : IJsonPropertySerializer<EffectorModule>
{
    public readonly static string EffectorTypePropertyName = "$EffectorType";

    public string ToJson(object obj)
    {
        if (obj == null) return "null";
        if (obj is not EffectorModule module) throw new ArgumentException("The object to serialize is not of type EffectorModule");

        StringBuilder json = new StringBuilder(1024);
        JsonSerializerUtility.BeginObject(json);
        JsonSerializerUtility.SerializeDefault(json, EffectorTypePropertyName, module.ModuleData.GetType().Name);
        JsonSerializerUtility.SerializeDefault(json, nameof(module.Enabled), module.Enabled);
        JsonSerializerUtility.SerializeDefault(json, nameof(module.Locked), module.Locked);
        JsonSerializerUtility.SerializeDefault(json, nameof(module.ModuleData), module.ModuleData);
        JsonSerializerUtility.SerializeDefault(json, nameof(module.QuickToggleStates), module.QuickToggleStates);
        JsonSerializerUtility.EndObject(json);
        return json.ToString();
    }

    public object FromJson(string json, Type type, bool ignoreUnknownProperties = false)
    {
        if (type != typeof(EffectorModule)) throw new ArgumentException("Type should be EffectorModule");
        var properties = JsonSerializerUtility.GetProperties(json);

        if (!properties.TryGetValue(EffectorTypePropertyName, out string typeJson))
            throw new JsonSerializerException($"EffectorModule should have {EffectorTypePropertyName} property");

        string effectorTypeName = (string)DefaultJsonSerializer.Default.FromJson(typeJson, typeof(string));
        Type effectorType = GetType().Assembly.GetType(effectorTypeName);
        bool isEffector = effectorType?.GetInterface(nameof(IParticleEffector)) is { };
        bool isProxy = effectorType?.GetInterface(nameof(IParticleEffectorProxy)) is { };
        if (effectorType is null || (!isEffector && !isProxy)) 
            throw new JsonSerializerException($"The effector type {effectorTypeName} is invalid");

        EffectorModule module;
        if (properties.TryGetValue(nameof(EffectorModule.ModuleData), out string moduleDataJson))
            module = EffectorModule.FromModuleData(DefaultJsonSerializer.Default.FromJson(moduleDataJson, effectorType));
        else
            module = new EffectorModule(effectorType);

        if (properties.TryGetValue(nameof(module.Enabled), out string enabled))
            module.Enabled = (bool)DefaultJsonSerializer.Default.FromJson(enabled, typeof(bool));
        if (properties.TryGetValue(nameof(module.Locked), out string locked))
            module.Locked = (bool)DefaultJsonSerializer.Default.FromJson(locked, typeof(bool));
        if (properties.TryGetValue(nameof(module.QuickToggleStates), out string toggleStates)) {
            module.QuickToggleStates = (List<int>)DefaultJsonSerializer.Default.FromJson(toggleStates, typeof(List<int>));
            // filter values
            int toggleCount = module.GetQuickToggles().Count;
            while (module.QuickToggleStates.Count < toggleCount)
                module.QuickToggleStates.Add(0);
            module.QuickToggleStates.RemoveRange(toggleCount, module.QuickToggleStates.Count - toggleCount);
        }

        return module;
    }

    public void FromJsonOverwrite(string json, object obj, bool ignoreUnknownProperties = false) {
        throw new NotImplementedException();
    }
}
