using UnityEngine;
using static System.Math;
using static Core.MathUtility;

public class Particle
{
    public System.Func<Particle, float> VelocityDelegate { get; set; }
    public Vector3 Velocity { get; set; }
    public bool Visible { get; set; } // needed?
    public Color Color { get; set; }
    public Sprite Sprite { get; set; }
    public Vector3 Position { get; set; }
    public float Size { get; set; }

    public void SetRandomVelocity(float minAngle = Angle0, float maxAngle = Angle360)
    {
        float angle = Random.Range(minAngle, maxAngle);
        float magnitude = VelocityDelegate(this);
        Velocity = new Vector3((float)Cos(angle) * magnitude, (float)Sin(angle) * magnitude, 0);
    }
}
