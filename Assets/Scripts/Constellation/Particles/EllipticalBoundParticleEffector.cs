using UnityCore;
using UnityEngine;
using static Core.MathUtility;

public sealed class EllipticalBoundParticleEffector : BoundsParticleEffector
{
    public override string Name { get; set; } = "Elliptical Bounds";

    private float _hBaseSquare;
    private float _vBaseSquare;

    public override void AffectParticle(Particle p)
    {
        float ellipseLocator = p.Position.x * p.Position.x / _hBaseSquare + p.Position.y * p.Position.y / _vBaseSquare;
        if (ellipseLocator < 1) return;

        float tangentXRaw = -1 / (_vBaseSquare * p.Position.x);
        float tangentYRaw = 1 / (_hBaseSquare * p.Position.y);
        Vector2 tangent = new Vector2(tangentXRaw, tangentYRaw).normalized;
        float sign = p.Position.x * p.Position.y > 0f ? 1f : -1f;
        float normalX = -tangent.y * sign; // inward normal
        float normalY = tangent.x * sign;

        float directionFactor = normalX * p.Velocity.x + normalY * p.Velocity.y; // Dot(normal, p.Velocity)
        if (directionFactor >= 0) return;

        Vector2 normal = new Vector2(normalX, normalY);
        float tangentWeight, normalWeight, tangentFraction;
        switch (_bounceType) {
            case BoundsBounceType.RandomBounce:
                tangentWeight = Random.value * 2 - 1;
                normalWeight = System.MathF.Sqrt(1 - tangentWeight * tangentWeight);
                p.SetVelocityDirection(normalWeight * normal + tangentWeight * tangent);
                break;
            case BoundsBounceType.ElasticBounce:
                tangentFraction = tangent.x * p.Velocity.x + tangent.y * p.Velocity.y;
                p.Velocity = (-p.Velocity + 2 * tangentFraction * (Vector3)tangent) * _restitution;
                break;
            case BoundsBounceType.HybridBounce:
                tangentWeight = Random.value * 2 - 1;
                normalWeight = System.MathF.Sqrt(1 - tangentWeight * tangentWeight);
                tangentFraction = tangent.x * p.Velocity.x + tangent.y * p.Velocity.y;
                Vector3 elasticComponent = (-p.Velocity + 2 * tangentFraction * (Vector3)tangent) * _restitution;
                p.SetVelocityDirection(normalWeight * normal + tangentWeight * tangent);
                p.Velocity = p.Velocity * _randomFraction + elasticComponent * (1 - _randomFraction);
                break;
            case BoundsBounceType.Wrap:
                p.Position = new Vector3(-p.Position.x, -p.Position.y);
                break;
        }
    }

    public override bool InBounds(Vector2 position) {
        return position.x * position.x / _hBaseSquare + position.y * position.y / _vBaseSquare <= 1;
    }

    public override Vector2 SamplePoint() {
        return Random.insideUnitCircle * new Vector2(_horizontalBase, _verticalBase);
    }

    protected override void RecalculateBounds() {
        base.RecalculateBounds();

        _hBaseSquare = _horizontalBase * _horizontalBase;
        _vBaseSquare = _verticalBase * _verticalBase;
    }

    public override void RenderControls(ControlType controlTypes)
    {
        bool hasControl = controlTypes.HasFlag(ControlType.Interactable);
        if (!controlTypes.HasFlag(ControlType.Visualizers) && !hasControl) return;
        if (!ShowBounds && !ForceShowBounds && !hasControl) return;

        Vector2 radius = new Vector2(HorizontalBase, VerticalBase);
        Vector2 newCorner = GraphicControls.EllipseRadius(Vector2.zero, radius, BoundsColor, interactable: hasControl);
        
        if (newCorner != radius) BoundsFromHalfSize(newCorner);
    }
}
