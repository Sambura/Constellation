using System;

/// <summary>
/// Used to group similar effectors (mostly just useful for UI)
/// </summary>
public interface IParticleEffectorProxy
{
    public IParticleEffector Effector { get; set; }
    public event Action<IParticleEffector> EffectorChanged;

    public void InitProxy();
}
