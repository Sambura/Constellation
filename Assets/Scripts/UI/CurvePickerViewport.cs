using UnityEngine;
using Core;

public class CurvePickerViewport : UIEvents
{
	[SerializeField] private float _curveQuality = 1;
	[SerializeField] private UILineRenderer _lineRenderer;

	private FastList<Vector2> _points = new FastList<Vector2>();
	private AnimationCurve _curve;

	public RectTransform RectTransform { get; private set; }

	public AnimationCurve Curve
	{
		get => _curve;
		set
		{
			_curve = value;
			RedrawCurve();
		}
	}

	private void Awake() { RectTransform = GetComponent<RectTransform>(); }

	private void RedrawCurve()
	{
		if (_curve == null || _curve.length == 0)
		{
			_lineRenderer.LocalPoints = null;
			return;
		}

		_points.PseudoClear();

		float width = RectTransform.rect.width;
		float step = Mathf.Clamp(1 / _curveQuality / width, 0.00001f, float.MaxValue);
		for (float t = 0; t <= 1; t += step)
			_points.Add(new Vector2(t, _curve.Evaluate(t)));

		_lineRenderer.SetNormalizedPoints(_points);
	}
}
