using Core.Json;
using Core;
using System.Collections.Generic;

public class EffectorModule : ModuleDescriptor
{
    [NoJsonSerialization] public ParticleController Controller { get; set; }
    [NoJsonSerialization] public IParticleEffector Effector { get; private set; }
    [NoJsonSerialization] public IParticleEffectorProxy Proxy { get; private set; }

    private List<(string icon, int stateCount, object data)> _quickToggles;

    public EffectorModule(object effectorType) {
        ModuleData = System.Activator.CreateInstance(effectorType as System.Type);
        Initialize();
    }

    private EffectorModule() { }

    public static EffectorModule FromModuleData(object moduleData) {
        EffectorModule module = new() { ModuleData = moduleData };
        module.Initialize();

        return module;
    }

    private void Initialize() {
        if (ModuleData is IParticleEffectorProxy proxy) Proxy = proxy;
        if (Proxy is null)
            Effector = ModuleData as IParticleEffector;
        else
        {
            Proxy.InitProxy();
            Effector = Proxy.Effector;
            Proxy.EffectorChanged += OnEffectorChanged;
        }

        Enabled = true;
        Locked = false;
        HasProperties = true;
        Name = (Proxy as IParticleEffector)?.Name ?? Effector.Name;
        QuickToggleStates = new List<int>();

        for (int i = 0; i < GetQuickToggles().Count; i++) {
            (string icon, int stateCount, object data) toggle = GetQuickToggles()[i];
            QuickToggleStates.Add(Effector.DefaultControlType.HasFlag((ControlType)toggle.data) ? 1 : 0);
        }
    }

    private void OnEffectorChanged(IParticleEffector effector) {
        Effector = effector;
        Controller.ParticleEffectors = Controller.ParticleEffectors;
    }

    public override List<(string icon, int stateCount, object data)> GetQuickToggles() {
        if (_quickToggles is null)
        {
            _quickToggles = new();

            if (Effector.ControlType.HasFlag(ControlType.Visualizers))
                _quickToggles.Add(("VisualsIcon", 2, ControlType.Visualizers));

            if (Effector.ControlType.HasFlag(ControlType.Interactable))
                _quickToggles.Add(("MoveIcon2", 2, ControlType.Interactable));
        }

        return _quickToggles;
    }
}
