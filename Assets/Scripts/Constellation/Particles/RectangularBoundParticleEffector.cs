using UnityEngine;
using static Core.MathUtility;

public sealed class RectangularBoundParticleEffector : BoundsParticleEffector
{
    public override string Name { get; set; } = "Rectangular Bounds";

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

        switch (_bounceType) {
            case BoundsBounceType.RandomBounce:
                if (horizontalMisdirection)
                    p.SetRandomVelocity(leftHit ? -Angle90 : Angle90, leftHit ? Angle90 : Angle270);
                if (verticalMisdirection)
                    p.SetRandomVelocity(bottomHit ? Angle0 : Angle180, bottomHit ? Angle180 : Angle360);
                break;
            case BoundsBounceType.ElasticBounce:
                if (!horizontalMisdirection && !verticalMisdirection) return;

                p.Velocity = new Vector2(horizontalMisdirection ? -p.Velocity.x : p.Velocity.x, 
                    verticalMisdirection ? -p.Velocity.y : p.Velocity.y) * _restitution;
                break;
            case BoundsBounceType.HybridBounce:
                if (!horizontalMisdirection && !verticalMisdirection) return;

                Vector3 elasticComponent = new Vector2(horizontalMisdirection ? -p.Velocity.x : p.Velocity.x,
                    verticalMisdirection ? -p.Velocity.y : p.Velocity.y) * _restitution;
                if (horizontalMisdirection)
                    p.SetRandomVelocity(leftHit ? -Angle90 : Angle90, leftHit ? Angle90 : Angle270);
                if (verticalMisdirection)
                    p.SetRandomVelocity(bottomHit ? Angle0 : Angle180, bottomHit ? Angle180 : Angle360);
                p.Velocity = p.Velocity * _randomFraction + elasticComponent * (1 - _randomFraction);
                break;
            case BoundsBounceType.Wrap:
                if (!horizontalMisdirection && !verticalMisdirection) return;

                // I advise not to pry into this
                if (horizontalHit && verticalHit && (horizontalMisdirection ^ verticalMisdirection))
                    p.Position = new Vector3(p.Position.x, -p.Position.y);
                else
                    p.Position = new Vector3(-p.Position.x, -p.Position.y);
                break;
        }
    }

    public override bool InBounds(Vector2 position) {
        return position.x >= _left && position.x <= _right && position.y >= _bottom & position.y <= _top;
    }

    protected override void RecalculateBounds() {
        base.RecalculateBounds();

        _left = -_horizontalBase;
        _right = _horizontalBase;
        _bottom = -_verticalBase;
        _top = _verticalBase;
    }

    public override void RenderControls(ControlType controlTypes)
    {
        bool hasControl = controlTypes.HasFlag(ControlType.Interactable);
        if (!controlTypes.HasFlag(ControlType.Visualizers) && !hasControl) return;
        if (!ShowBounds && !ForceShowBounds && !hasControl) return;

        Vector2 newCorner = Vector2.zero;
        Vector2 current = new Vector2(HorizontalBase, VerticalBase);
        if (hasControl) {
            newCorner = GraphicControls.DragSquare(current, BoundsColor, angleDegrees: 180);
        }

        (float left, float right, float bottom, float top) = (-HorizontalBase, HorizontalBase, -VerticalBase, VerticalBase);
        float leftDelta = GraphicControls.Line(left, bottom, left, top, BoundsColor, interactable: hasControl);
        float rightDelta = GraphicControls.Line(right, bottom, right, top, BoundsColor, interactable: hasControl);
        float bottomDelta = GraphicControls.Line(left, bottom, right, bottom, BoundsColor, interactable: hasControl);
        float topDelta = GraphicControls.Line(left, top, right, top, BoundsColor, interactable: hasControl);

        if (!hasControl) return;
        Vector2 sideDelta = new Vector2(leftDelta - rightDelta, topDelta - bottomDelta);
        newCorner += sideDelta;
        if (newCorner != current) BoundsFromHalfSize(newCorner);
    }
}