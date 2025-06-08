/// <summary>
/// Interface for particle effectors. Particle effectors are entities that affect particles' position and/or
/// velocity according to their set of rules. Effectors can be global - affect all particles equally, or 
/// local - affect only particles in some area, or a combination if applicable. Examples of effectors can be:
///     - Bounds (Effectors that simulate bounds, keeping particles restricted to a certain area)
///     - Friction effector (slows down all particles with time)
///     - Distortion effector (distorts particles' trajectories depending on their position)
///     - Kinematic effector (the very fact that particles are moving every frame in their velocity direction can be implemented as effector)
/// </summary>
public interface IParticleEffector
{
    /// <summary>
    /// Human-readable name of this effector
    /// </summary>
    public string Name { get; set; }
    public bool Initialized { get; }
    public ControlType ControlType { get; }
    public ControlType DefaultControlType { get; }
    public EffectorType EffectorType { get; }

    /// <summary>
    /// Initialize the effector state from the ParticleController
    /// </summary>
    public void Init(ParticleController controller);
    /// <summary>
    /// Affect the given particle
    /// </summary>
    public void AffectParticle(Particle p);
    /// <summary>
    /// Destructor method to call to detach from ParticleController's events
    /// </summary>
    public void Detach();
    /// <summary>
    /// Displays controls for this effector, as well as handles provided mouse input.
    /// Returns true if this effector was changed by the current mouse input.
    /// </summary>
    public void RenderControls(ControlType controlTypes);
}

[System.Flags]
public enum ControlType
{
    None = 0,
    Interactable = 1,
    Visualizers = 2,
    Both = 3,
}

/// <summary>
/// PerParticle - implements non-trivial AffectParticle method
/// </summary>
[System.Flags]
public enum EffectorType
{
    Passive = 0,
    PerParticle = 1
}
