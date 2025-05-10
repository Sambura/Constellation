using Core.Json;
using Core;

public class EffectorModule : ModuleDescriptor
{
    [NoJsonSerialization] public ParticleController Controller { get; set; }
    [NoJsonSerialization] public IParticleEffector Effector { get; private set; }
    [NoJsonSerialization] public IParticleEffectorProxy Proxy { get; private set; }

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
    }

    private void OnEffectorChanged(IParticleEffector effector) {
        Effector = effector;
        Controller.ParticleEffectors = Controller.ParticleEffectors;
    }
}
