using UnityEngine;
using ConstellationUI;

public class InterationTabPresenter : MonoBehaviour 
{
	[Header("General")]
	[SerializeField] private InteractionCore _interactionCore;

	[Header("Interaction parameters")]
	[SerializeField] private Slider _attractionOrderSlider;
	[SerializeField] private Slider _attractionStrenghtSlider;
	[SerializeField] private Slider _attractionAssertionSlider;

	private void Start()
	{
		// UI initialization
		_attractionOrderSlider.Value = _interactionCore.AttractionOrder;
		_attractionStrenghtSlider.Value = _interactionCore.AttractionStrength;
		_attractionAssertionSlider.Value = _interactionCore.AttractionAssertion;

		// Set up event listeners
		_attractionOrderSlider.ValueChanged += OnAttractionOrderChanged;
		_interactionCore.AttractionOrderChanged += OnAttractionOrderChanged;

		_attractionStrenghtSlider.ValueChanged += OnAttractionStrengthChanged;
		_interactionCore.AttractionStrengthChanged += OnAttractionStrengthChanged;

		_attractionAssertionSlider.ValueChanged += OnAttractionAssertionChanged;
		_interactionCore.AttractionAssertionChanged += OnAttractionAssertionChanged;
	}

	private void OnAttractionOrderChanged(float value)
	{
		_interactionCore.AttractionOrder = value;
		_attractionOrderSlider.SetValueWithoutNotify(value);
	}

	private void OnAttractionStrengthChanged(float value)
	{
		_interactionCore.AttractionStrength = value;
		_attractionStrenghtSlider.SetValueWithoutNotify(value);
	}

	private void OnAttractionAssertionChanged(float value)
	{
		_interactionCore.AttractionAssertion = value;
		_attractionAssertionSlider.SetValueWithoutNotify(value);
	}
}
