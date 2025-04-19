using ConfigSerialization;
using UnityEngine;

/// <summary>
/// This class holds any metadata that needs to be shipped with a simulation config file.
/// Currently it only specifies program version used to generate the config.
/// </summary>
class SimulationConfigMetadata
{
    [ConfigProperty(name: "ConfigVersion", hasEvent: false, AllowPolling = false)]
    public string Version { get; set; } = null;

    public static SimulationConfigMetadata Default => new SimulationConfigMetadata { Version = Application.version };
}
