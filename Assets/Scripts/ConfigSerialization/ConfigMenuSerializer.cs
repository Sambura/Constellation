using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ConstellationUI;

namespace ConfigSerialization
{
	public class ConfigMenuSerializer : MonoBehaviour
	{
		[SerializeField] private Transform _uiParent;

		[Header("Prefabs")]
		[SerializeField] private GameObject _sliderPrefab;
		[SerializeField] private GameObject _togglePrefab;
		[SerializeField] private GameObject _colorButtonPrefab;
		[SerializeField] private GameObject _gradientButtonPrefab;
		[SerializeField] private GameObject _curveButtonPrefab;
		[SerializeField] private GameObject _minMaxSliderPrefab;
		[SerializeField] private GameObject _radioButtonPrefab;
		[SerializeField] private GameObject _buttonPrefab;
		[SerializeField] private GameObject _dropdownPrefab;

		public List<object> ConfigContainers;

		private static readonly Type[] IntegralTypes = new[] { typeof(int), typeof(uint),
													   typeof(short), typeof(ushort),
													   typeof(long), typeof(ulong),
													   typeof(byte) };

		public static bool IsIntegral(Type type)
		{
			return IntegralTypes.Contains(type);
		}

		public void Serialize()
		{
			HashSet<string> handledProperties = new HashSet<string>();

			foreach (var container in ConfigContainers)
			{
				Type type = container.GetType();
				PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				EventInfo[] events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public);
				foreach (PropertyInfo property in properties)
				{
					if (handledProperties.Contains(property.Name)) continue;

					ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
					if (configProperty == null) continue;
					string eventName = property.Name + "Changed";
					EventInfo @event = null;
					if (configProperty.HasEvent)
					{
						try
						{
							@event = events.First(x => x.Name == eventName);
						}
						catch
						{
							Debug.LogError($"Config UI serialization failed :: event not found for property {property.Name} for container {type.Name}");
							continue;
						}
					}

					CreatePropertyControl(property, @event, container)?.ForEach(x => handledProperties.Add(x));
				}

				MethodInfo[] methods = type.GetMethods();
				foreach (MethodInfo method in methods)
				{
					InvokableMethod invokableMethod = method.GetCustomAttribute<InvokableMethod>();
					if (invokableMethod == null) continue;

					CreateMethodControl(method, container);
				}
			}
		}

		private void CreateMethodControl(MethodInfo method, object container)
		{
			InvokableMethod invokableMethod = method.GetCustomAttribute<InvokableMethod>();
			GameObject newControl = Instantiate(_buttonPrefab, _uiParent);
			Button button = newControl.GetComponent<Button>();
			button.TextLabel = invokableMethod.Name ?? SplitAndLowerCamelCase(method.Name);

			button.Click += () => method.Invoke(container, Array.Empty<object>());
		}

		private List<string> CreatePropertyControl(PropertyInfo property, EventInfo @event, object container) {
			Type type = property.PropertyType;

			if (type.IsEnum)
				return CreateDropdownList(property, @event, container);
			if (IsIntegral(type) || type == typeof(float))
				return CreateSlider(property, @event, container);
			else if (type == typeof(bool))
				return CreateToggle(property, @event, container);
			else if (type == typeof(Color))
				return CreateColorButton(property, @event, container);
			else if (type == typeof(Gradient))
				return CreateGradientButton(property, @event, container);
			else if (type == typeof(AnimationCurve))
				return CreateCurveButton(property, @event, container);
			else
				Debug.LogError($"Failed to create control for {property} on {container} : not implemented!");

			return null;
		}

		private List<string> CreateDropdownList(PropertyInfo property, EventInfo @event, object container)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			DropdownListProperty dropdownProperty = configProperty as DropdownListProperty;
			Type configType = configProperty.GetType();

			if (dropdownProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attempt to make a dropdown list on property with {configType} attribute");
				return null;
			}

			if (!property.PropertyType.IsEnum) throw new NotImplementedException("Currently only enums properties can have dropdown list");

			GameObject newControl = Instantiate(_dropdownPrefab, _uiParent);
			DropdownList dropdown = newControl.GetComponent<DropdownList>();
			dropdown.TextLabel = configProperty.Name ?? SplitAndLowerCamelCase(property.Name);

			List<string> options = new List<string>();
			string[] enumNames = property.PropertyType.GetEnumNames();
			// mapping[0] == enum index that corresponds to the first displayed option
			List<int> mapping = new List<int>();

			if (dropdownProperty?.DisplayedOptions != null)
			{
				string[] names = dropdownProperty.OptionNames;

				foreach (object option in dropdownProperty.DisplayedOptions)
				{
					if (option.GetType() != property.PropertyType) throw new ArgumentException("Dropdown displayed options contain invalid types");
					int optionIndex = Convert.ToInt32(option);
					options.Add(names?[mapping.Count] ?? SplitAndLowerCamelCase(option.ToString()));
					mapping.Add(optionIndex);
				}
			}
			else
			{
				string[] names = dropdownProperty?.OptionNames;

				for (int i = 0; i < (names ?? enumNames).Length; i++)
				{
					options.Add(names?[i] ?? SplitAndLowerCamelCase(enumNames[i]));
					mapping.Add(i);
				}
			}

			dropdown.SetOptions(options);
			dropdown.SelectedValue = mapping.IndexOf((int)property.GetValue(container));

			Action<int> dropdownHandler = x => property.SetValue(container, mapping[x]);
			dropdown.SelectedValueChanged += dropdownHandler;

			if (configProperty.HasEvent)
			{
				MethodInfo getDelegate = GetType().GetMethod(nameof(GetDropdownContainerDelegate), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod(property.PropertyType);
				Delegate containerHandler = (Delegate)getDelegate.Invoke(null, new object[] { dropdown, mapping });

				@event.AddEventHandler(container, containerHandler);
			}

			return new List<string>() { property.Name };
		}

		private static Delegate GetDropdownContainerDelegate<T>(DropdownList dropdown, List<int> mapping)
		{
			// I use IndexOf() to search for the needed mapping instead of creating an inverse mapping list
			// since this is a really rare operation I don't think it is worth to spend additional memory on it
			return (Action<T>)(x => dropdown.SelectedValue = mapping.IndexOf(Convert.ToInt32(x)));
		}

		private List<string> CreateCurveButton(PropertyInfo property, EventInfo @event, object container)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			CurvePickerButtonProperty curveProperty = configProperty as CurvePickerButtonProperty;
			Type configType = configProperty.GetType();

			if (curveProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			GameObject newControl = Instantiate(_curveButtonPrefab, _uiParent);
			CurvePickerButton curveButton = newControl.GetComponent<CurvePickerButton>();

			AnimationCurve value = (AnimationCurve)property.GetValue(container);

			curveButton.TextLabel = configProperty.Name ?? SplitAndLowerCamelCase(property.Name);
			curveButton.DialogTitle = curveProperty?.DialogTitle ?? $"Modify {curveButton.TextLabel}";
			curveButton.Curve = value;

			Action<AnimationCurve> handler = (AnimationCurve x) =>
			{
				property.SetValue(container, x);
				curveButton.Curve = x;
			};

			curveButton.CurveChanged += handler;
			if (configProperty.HasEvent) @event.AddEventHandler(container, handler);

			return new List<string>() { property.Name };
		}

		private List<string> CreateGradientButton(PropertyInfo property, EventInfo @event, object container)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			GradientPickerButtonProperty gradientProperty = configProperty as GradientPickerButtonProperty;
			Type configType = configProperty.GetType();

			if (gradientProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			GameObject newControl = Instantiate(_gradientButtonPrefab, _uiParent);
			GradientPickerButton gradientButton = newControl.GetComponent<GradientPickerButton>();

			Gradient value = (Gradient)property.GetValue(container);

			gradientButton.TextLabel = configProperty.Name ?? SplitAndLowerCamelCase(property.Name);
			gradientButton.DialogTitle = gradientProperty?.DialogTitle ?? $"Modify {SplitCamelCase(property.Name)}";
			gradientButton.Gradient = value;

			Action<Gradient> handler = (Gradient x) =>
			{
				property.SetValue(container, x);
				gradientButton.Gradient = x;
			};

			gradientButton.GradientChanged += handler;
			if (configProperty.HasEvent) @event.AddEventHandler(container, handler);

			return new List<string>() { property.Name };
		}

		private List<string> CreateColorButton(PropertyInfo property, EventInfo @event, object container)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			ColorPickerButtonProperty colorProperty = configProperty as ColorPickerButtonProperty;
			Type configType = configProperty.GetType();

			if (colorProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			GameObject newControl = Instantiate(_colorButtonPrefab, _uiParent);
			ColorPickerButton colorButton = newControl.GetComponent<ColorPickerButton>();

			Color value = (Color)property.GetValue(container);

			colorButton.TextLabel = configProperty.Name ?? SplitAndLowerCamelCase(property.Name);
			colorButton.UseAlpha = colorProperty?.UseAlpha ?? true;
			colorButton.DialogTitle = colorProperty?.DialogTitle ?? $"Select {SplitCamelCase(property.Name)}";
			colorButton.Color = value;

			Action<Color> handler = (Color x) =>
			{
				property.SetValue(container, x);
				colorButton.Color = x;
			};

			colorButton.ColorChanged += handler;
			if (configProperty.HasEvent) @event.AddEventHandler(container, handler);

			return new List<string>() { property.Name };
		}

		private List<string> CreateToggle(PropertyInfo property, EventInfo @event, object container)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			RadioButtonsProperty radioButtonProperty = configProperty as RadioButtonsProperty;
			Type configType = configProperty.GetType();

			if (radioButtonProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			if (radioButtonProperty != null)
				return CreateRadioButtons(property, @event, container);

			GameObject newControl = Instantiate(_togglePrefab, _uiParent);
			Toggle toggle = newControl.GetComponent<Toggle>();

			bool value = (bool)property.GetValue(container);

			toggle.IsChecked = value;
			toggle.TextLabel = SplitAndLowerCamelCase(property.Name);
			
			Action<bool> handler = (bool x) =>
			{
				property.SetValue(container, x);
				toggle.SetIsCheckedWithoutNotify(x);
			};

			toggle.IsCheckedChanged += handler;
			if (configProperty.HasEvent) @event.AddEventHandler(container, handler);

			return new List<string>() { property.Name };
		}

		private List<string> CreateRadioButtons(PropertyInfo property, EventInfo @event, object container)
		{
			RadioButtonsProperty radioButtonsProperty = property.GetCustomAttribute<RadioButtonsProperty>();
			if (property.PropertyType == typeof(bool) && radioButtonsProperty.RadioNames.Length != 2)
				throw new ArgumentException("Can only create 2 radio buttons for bool property");

			GameObject subContainer = new GameObject($"{property.Name} radio group");
			RectTransform transform = subContainer.AddComponent<RectTransform>();
			transform.SetParent(_uiParent, false);
			transform.anchorMin = new Vector2(0, 1);
			transform.anchorMax = new Vector2(1, 1);
			transform.pivot = new Vector2(0.5f, 1);
			var toggleGroup = subContainer.AddComponent<UnityEngine.UI.ToggleGroup>();
			toggleGroup.allowSwitchOff = false;
			VerticalUIStack verticalStack = subContainer.AddComponent<VerticalUIStack>();
			verticalStack.BottomMargin = verticalStack.TopMargin = verticalStack.Spacing = 2;

			List<Toggle> radioButtons = new List<Toggle>();
			foreach (string name in radioButtonsProperty.RadioNames)
			{
				GameObject newControl = Instantiate(_radioButtonPrefab, transform);
				Toggle toggle = newControl.GetComponent<Toggle>();
				radioButtons.Add(toggle);
				toggle.IsChecked = false;
				toggle.TextLabel = name;
				toggle.ToggleGroup = toggleGroup;
			}

			bool value = (bool)property.GetValue(container);

			radioButtons[Convert.ToInt32(value)].IsChecked = true;

			for (int i = 0; i < radioButtons.Count - 1; i++)
			{
				Action<bool> handler = (bool x) =>
				{
					property.SetValue(container, i == 0 ? x : !x);
				};

				radioButtons[i].IsCheckedChanged += handler;
			}

			if (radioButtonsProperty.HasEvent)
			{
				Action<bool> commonHandler = (bool x) =>
				{
					Debug.Log(Convert.ToInt32(x));
					radioButtons[Convert.ToInt32(x)].IsChecked = true;
					radioButtons[1 - Convert.ToInt32(x)].IsChecked = false;
				};

				@event.AddEventHandler(container, commonHandler);
			}

			return new List<string>() { property.Name };
		}

		private List<string> CreateSlider(PropertyInfo property, EventInfo @event, object container)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			SliderProperty sliderProperty = configProperty as SliderProperty;
			Type configType = configProperty.GetType();

			if (sliderProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			if (sliderProperty is MinMaxSliderProperty)
				return CreateMinMaxSlider(property, container);

			GameObject newControl = Instantiate(_sliderPrefab, _uiParent);
			SliderWithText slider = newControl.GetComponent<SliderWithText>();
			bool isInt = IsIntegral(property.PropertyType);

			int intValue = isInt ? (int)property.GetValue(container) : 0;
			float floatValue = isInt ? intValue : (float)property.GetValue(container);

			slider.MinValue = sliderProperty?.MinValue ?? float.MinValue;
			slider.MaxValue = sliderProperty?.MaxValue ?? float.MaxValue;
			slider.MinSliderValue = sliderProperty?.MinSliderValue ?? (isInt ? intValue - 100 : floatValue - 5);
			slider.MaxSliderValue = sliderProperty?.MaxSliderValue ?? (isInt ? intValue + 100 : floatValue + 5);
			slider.InputFormatting = sliderProperty?.InputFormatting ?? (isInt ? "0" : "0.000");
			slider.InputRegex = sliderProperty?.InputRegex ?? @"([-+]?[0-9]*\.?[0-9]+)";
			slider.RegexGroupIndex = sliderProperty?.RegexGroupIndex ?? 1;
			slider.TextLabel = configProperty.Name ?? SplitAndLowerCamelCase(property.Name);
			slider.Value = floatValue; // Currently sliders work with floats only

			Delegate handler;
			if (isInt)
			{
				Action<int> intHandler = (int x) =>
				{
					property.SetValue(container, x);
					slider.SetValueWithoutNotify(x);
				};
				slider.IntValueChanged += intHandler;
				handler = intHandler;
			} else
			{
				Action<float> floatHandler = (float x) =>
				{
					property.SetValue(container, x);
					slider.SetValueWithoutNotify(x);
				};
				slider.ValueChanged += floatHandler;
				handler = floatHandler;
			}

			if (configProperty.HasEvent) @event.AddEventHandler(container, handler);

			return new List<string>() { property.Name };
		}

		private List<string> CreateMinMaxSlider(PropertyInfo property, object container)
		{
			MinMaxSliderProperty sliderProperty = property.GetCustomAttribute<MinMaxSliderProperty>();
			if (sliderProperty.HigherPropertyName == null) return null;

			PropertyInfo lower = property;
			PropertyInfo higher = property.DeclaringType.GetProperty(sliderProperty.HigherPropertyName);
			EventInfo lowerEvent = GetEvent(lower), higherEvent = GetEvent(higher);

			GameObject newControl = Instantiate(_minMaxSliderPrefab, _uiParent);
			MinMaxSliderWithInput slider = newControl.GetComponent<MinMaxSliderWithInput>();

			bool isInt = IsIntegral(lower.PropertyType);

			int lowerIntValue = isInt ? (int)lower.GetValue(container) : 0;
			float lowerFloatValue = isInt ? lowerIntValue : (float)lower.GetValue(container);
			int higherIntValue = isInt ? (int)higher.GetValue(container) : 0;
			float higherFloatValue = isInt ? higherIntValue : (float)higher.GetValue(container);

			slider.MinSliderValue = sliderProperty.MinSliderValue;
			slider.MaxSliderValue = sliderProperty.MaxSliderValue;
			slider.MinValue = sliderProperty.MinValue;
			slider.MaxValue = sliderProperty.MaxValue;
			slider.InputFormatting = sliderProperty.InputFormatting ?? (isInt ? "0" : "0.000");
			slider.InputRegex = sliderProperty.InputRegex ?? @"([-+]?[0-9]*\.?[0-9]+)";
			slider.RegexGroupIndex = sliderProperty.RegexGroupIndex ?? 1;
			slider.TextLabel = sliderProperty.Name ?? SplitAndLowerCamelCase(lower.Name);
			slider.LowerLabel = sliderProperty.LowerLabel ?? "Min";
			slider.HigherLabel = sliderProperty.HigherLabel ?? "Max";
			slider.MinMaxSpacing = sliderProperty.MinMaxSpacing;
			slider.LowerValue = lowerFloatValue;
			slider.HigherValue = higherFloatValue;

			if (isInt)
			{
				Action<int> intLowerHandler = (int x) =>
				{
					lower.SetValue(container, x);
					slider.SetLowerValueWithoutNotify(x);
				};
				slider.IntLowerValueChanged += intLowerHandler;
				lowerEvent.AddEventHandler(container, intLowerHandler);

				Action<int> intHigherHandler = (int x) =>
				{
					higher.SetValue(container, x);
					slider.SetHigherValueWithoutNotify(x);
				};
				slider.IntHigherValueChanged += intHigherHandler;
				if (sliderProperty.HasEvent) higherEvent.AddEventHandler(container, intHigherHandler);
			}
			else
			{
				Action<float> floatLowerHandler = (float x) =>
				{
					lower.SetValue(container, x);
					slider.SetLowerValueWithoutNotify(x);
				};
				slider.LowerValueChanged += floatLowerHandler;
				lowerEvent.AddEventHandler(container, floatLowerHandler);

				Action<float> floatHigherHandler = (float x) =>
				{
					higher.SetValue(container, x);
					slider.SetHigherValueWithoutNotify(x);
				};
				slider.HigherValueChanged += floatHigherHandler;
				if (sliderProperty.HasEvent) higherEvent.AddEventHandler(container, floatHigherHandler);
			}

			return new List<string>() { lower.Name, higher.Name };

			static EventInfo GetEvent(PropertyInfo property) => property.DeclaringType.GetEvent(property.Name + "Changed");
		}

		public static string SplitAndLowerCamelCase(string str)
		{
			return str[0] + SplitCamelCase(str).ToLowerInvariant().Substring(1);
		}

		/*
		 * https://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
		 */
		public static string SplitCamelCase(string str)
		{
			return System.Text.RegularExpressions.Regex.Replace(
				System.Text.RegularExpressions.Regex.Replace(
					str,
					@"(\P{Ll})(\P{Ll}\p{Ll})",
					"$1 $2"
				),
				@"(\p{Ll})(\P{Ll})",
				"$1 $2"
			);
		}

		private void Start()
		{
			ConfigContainers = new List<object>()
			{
				//FindObjectOfType<ParticleController>(),
				//FindObjectOfType<MainVisualizer>(),
				//FindObjectOfType<FragmentationVisualization>(),
				//FindObjectOfType<InteractionCore>(),
				FindObjectOfType<ApplicationController>(),
				FindObjectOfType<MiscConfigCollection>(),
			};
			Serialize();
		}
	}
}
