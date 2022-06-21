using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Core;
using System;

public class CurvePicker : MonoDialog
{
	[SerializeField] private GameObject _nodePrefab;
	[SerializeField] private CurvePickerViewport _viewport;
	[SerializeField] private UILineRenderer _tangentRenderer;
	[SerializeField] private CurvePickerKnot _leftKnot;
	[SerializeField] private CurvePickerKnot _rightKnot;
	[SerializeField] private float _tangentLength = 1000f;

	private List<CurvePickerNode> _nodes;
	private CurvePickerNode _selectedNode;
	private AnimationCurve _curve;

	public CurvePickerNode SelectedNode
	{
		get => _selectedNode;
		set
		{
			if (_selectedNode == value) return;
			if (_selectedNode) _selectedNode.Selected = false;
			_selectedNode = value;
			if (_selectedNode)
			{
				_selectedNode.Selected = true;
				PlaceKnots();
				_leftKnot.Selected = false;
				_rightKnot.Selected = false;
			}
			else
			{
				_leftKnot.gameObject.SetActive(false);
				_rightKnot.gameObject.SetActive(false);
				_tangentRenderer.LocalPoints = null;
			}
		}
	}

	public AnimationCurve Curve
	{
		get => _curve;
		set
		{
			if (_curve == value) return;
			SelectedNode = null;
			_curve = null;

			while (_nodes.Count > value.length)
			{
				Destroy(_nodes[_nodes.Count - 1].gameObject);
				_nodes.RemoveAt(_nodes.Count - 1);
			}

			while (_nodes.Count < value.length)
				AddNode();
			
			for (int i = 0; i < value.length; i++)
			{
				Keyframe key = value[i];
				_nodes[i].SetNormalizedPosition(new Vector2(key.time, key.value));
				_nodes[i].Data = key;
				_nodes[i].Data.weightedMode = WeightedMode.Both;
			}
			_curve = value;

			// UpdateNodes invokes OnCurveChanged
			UpdateNodes();
		}
	}
	public event Action<AnimationCurve> CurveChanged;
	public Action<AnimationCurve> OnCurveChanged;

	private void CallOnCurveChanged(AnimationCurve curve) => OnCurveChanged?.Invoke(curve); 

	private void OnViewportPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			SelectedNode = null;
			return;
		}

		CurvePickerNode newNode = AddNode();
		newNode.Position = eventData.position;
		UpdateNodes();
		newNode.OnPointerDown(eventData);
	}

	private void OnViewportPointerUp(PointerEventData eventData)
	{
		SelectedNode?.OnPointerUp(eventData);
	}

	private CurvePickerNode AddNode()
	{
		CurvePickerNode newNode = Instantiate(_nodePrefab, _viewport.RectTransform).GetComponent<CurvePickerNode>();
		_nodes.Add(newNode);
		newNode.PositionChanged += OnNodePositionChanged;
		newNode.SelectedChanged += NodeSelectedChanged;

		return newNode;
	}

	private void NodeSelectedChanged(MonoSelectable node, bool value)
	{
		if (value) SelectedNode = (CurvePickerNode)node;
	}

	private void OnNodePositionChanged(Vector3 position)
	{
		UpdateNodes();
	}

	private void PlaceKnots()
	{
		_leftKnot.gameObject.SetActive(SelectedNode != _nodes[0]);
		_rightKnot.gameObject.SetActive(SelectedNode != _nodes[_nodes.Count - 1]);

		Vector3 position = SelectedNode.Position;

		if (_leftKnot.isActiveAndEnabled)
		{
			float leftAngle = Mathf.Atan(SelectedNode.Data.inTangent);
			float leftWeight = -_tangentLength * SelectedNode.Data.inWeight;
			_leftKnot.transform.position = position + new Vector3(leftWeight * Mathf.Cos(leftAngle),
																  leftWeight * Mathf.Sin(leftAngle));
		}

		if (_rightKnot.isActiveAndEnabled)
		{
			float rightAngle = Mathf.Atan(SelectedNode.Data.outTangent);
			float rightWeight = _tangentLength * SelectedNode.Data.outWeight;
			_rightKnot.transform.position = position + new Vector3(rightWeight * Mathf.Cos(rightAngle),
																   rightWeight * Mathf.Sin(rightAngle));
		}

		FastList<Vector2> points = new FastList<Vector2>(3);
		if (_leftKnot.isActiveAndEnabled) points.Add(_leftKnot.Position);
		points.Add(SelectedNode.Position);
		if (_rightKnot.isActiveAndEnabled) points.Add(_rightKnot.Position);
		if (points._count > 1)
			_tangentRenderer.SetWorldPoints(points.ToArray());
	}

	public void RemoveSelectedNode()
	{
		if (SelectedNode == null) return;

		_nodes.Remove(SelectedNode);
		Destroy(SelectedNode.gameObject);
		SelectedNode = null;

		UpdateNodes();
	}

	private void UpdateNodes()
	{
		if (Curve == null) return;

		// sort by x coordinate
		Algorithm.BubbleSort(_nodes, (x, y) =>
		{
			bool byX = x.Position.x != y.Position.x;
			return byX ? x.Position.x < y.Position.x : x.GetHashCode() < y.GetHashCode();
		});

		Keyframe[] keys = new Keyframe[_nodes.Count];
		for (int i = 0; i < _nodes.Count; i++) keys[i] = _nodes[i].Data;

		_curve = new AnimationCurve(keys);

		if (SelectedNode) PlaceKnots();

		_viewport.Curve = _curve;
		CurveChanged?.Invoke(_curve);
	}

	private void KnotPositionChanged(Vector3 position)
	{
		if (SelectedNode == null) return;

		if (_leftKnot.isActiveAndEnabled)
		{
			Vector3 leftKnot = _leftKnot.Position - SelectedNode.Position;
			SelectedNode.Data.inTangent = leftKnot.y / Mathf.Min(-0.0001f, leftKnot.x);
			SelectedNode.Data.inWeight = leftKnot.magnitude / _tangentLength;
		}
		if (_rightKnot.isActiveAndEnabled)
		{
			Vector3 rightKnot = _rightKnot.Position - SelectedNode.Position;
			SelectedNode.Data.outTangent = rightKnot.y / Mathf.Max(0.0001f, rightKnot.x);
			SelectedNode.Data.outWeight = rightKnot.magnitude / _tangentLength;
		}

		UpdateNodes();
	}

	private void KnotSelectedChanged(MonoSelectable knot, bool value)
	{
		if (value == false) return;
		(knot == _leftKnot ? _rightKnot : _leftKnot).Selected = false;
	}

	protected override void Awake()
	{
		base.Awake();

		_nodes = new List<CurvePickerNode>();

		_leftKnot.SelectedChanged += KnotSelectedChanged;
		_rightKnot.SelectedChanged += KnotSelectedChanged;

		_leftKnot.PositionChanged += KnotPositionChanged;
		_rightKnot.PositionChanged += KnotPositionChanged;

		_viewport.PointerDown += OnViewportPointerDown;
		_viewport.PointerUp += OnViewportPointerUp;

		CurveChanged += CallOnCurveChanged;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if (_leftKnot)
		{
			_leftKnot.SelectedChanged -= KnotSelectedChanged;
			_rightKnot.PositionChanged -= KnotPositionChanged;
		}
		if (_rightKnot)
		{
			_rightKnot.SelectedChanged -= KnotSelectedChanged;
			_leftKnot.PositionChanged -= KnotPositionChanged;
		}
		if (_viewport)
		{
			_viewport.PointerDown -= OnViewportPointerDown;
			_viewport.PointerUp -= OnViewportPointerUp;
		}
	}
}
