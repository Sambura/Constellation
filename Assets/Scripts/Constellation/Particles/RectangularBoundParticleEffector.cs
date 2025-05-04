using UnityEngine;
using static Core.MathUtility;
using static ParticleController;

public sealed class RectangularBoundParticleEffector : BoundParticleEffector
{
    public override string Name { get; set; } = "Rectangular Bounds Effector";

    private float _left;
    private float _right;
    private float _bottom;
    private float _top;

    public override void AffectParticle(Particle p)
    {
        bool leftHit = p.Position.x <= _left;
        bool rightHit = p.Position.x >= _right;
        bool bottomHit = p.Position.y <= _bottom;
        bool topHit = p.Position.y >= _top;

        bool horizontalHit = leftHit || rightHit;
        bool verticalHit = bottomHit || topHit;
        bool horizontalMisdirection = horizontalHit && p.Position.x * p.Velocity.x > 0;
        bool verticalMisdirection = verticalHit && p.Position.y * p.Velocity.y > 0;

        switch (BounceType) {
            case BoundsBounceType.RandomBounce:
                if (horizontalMisdirection)
                    p.SetRandomVelocity(leftHit ? -Angle90 : Angle90, leftHit ? Angle90 : Angle270);
                if (verticalMisdirection)
                    p.SetRandomVelocity(bottomHit ? Angle0 : Angle180, bottomHit ? Angle180 : Angle360);
                break;
            case BoundsBounceType.ElasticBounce:
                p.Velocity = new Vector2(horizontalMisdirection ? -p.Velocity.x : p.Velocity.x, 
                    verticalMisdirection ? -p.Velocity.y : p.Velocity.y) * Restitution;
                break;
            case BoundsBounceType.HybridBounce:
                Vector3 elasticComponent = new Vector2(horizontalMisdirection ? -p.Velocity.x : p.Velocity.x,
                    verticalMisdirection ? -p.Velocity.y : p.Velocity.y) * Restitution;
                if (horizontalMisdirection)
                    p.SetRandomVelocity(leftHit ? -Angle90 : Angle90, leftHit ? Angle90 : Angle270);
                if (verticalMisdirection)
                    p.SetRandomVelocity(bottomHit ? Angle0 : Angle180, bottomHit ? Angle180 : Angle360);
                p.Velocity = p.Velocity * RandomFraction + elasticComponent * (1 - RandomFraction);
                break;
            case BoundsBounceType.Wrap:
                if (horizontalMisdirection || verticalMisdirection) {
                    // I advise not to pry into this
                    if (horizontalHit && verticalHit && (horizontalMisdirection ^ verticalMisdirection))
                        p.Position = new Vector3(p.Position.x, -p.Position.y);
                    else
                        p.Position = new Vector3(-p.Position.x, -p.Position.y);
                }
                break;
        }
    }

    public override bool InBounds(Vector2 position) {
        return position.x >= _left && position.x <= _right && position.y >= _bottom & position.y <= _top;
    }

    protected override void OnBoundsUpdated() {
        base.OnBoundsUpdated();

        _left = -HorizontalBase;
        _right = HorizontalBase;
        _bottom = -VerticalBase;
        _top = VerticalBase;
    }
}