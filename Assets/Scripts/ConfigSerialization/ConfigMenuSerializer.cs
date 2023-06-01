using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ConstellationUI;
using ConfigSerialization.Structuring;

namespace ConfigSerialization
{
	public class ConfigMenuSerializer : MonoBehaviour
	{
		[SerializeField] private RectTransform _uiParent;

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
		[SerializeField] private GameObject _groupHeaderPrefab;

		public List<object> ConfigContainers;

		private static readonly Type[] IntegralTypes = new[] { typeof(int), typeof(uint),
													   typeof(short), typeof(ushort),
													   typeof(long), typeof(ulong),
													   typeof(byte) };

		public static bool IsIntegral(Type type)
		{
			return IntegralTypes.Contains(type);
		}

		private class Group
		{
			public string Name { get; set; }
			public string Id { get; set; }
			public int? LocalIndex { get; set; }
			public object DefiningContainer { get; set; }
			public Group Parent { get; set; }
			public string LastInvalidUpdatePropertyName { get; private set; }

			public List<Group> Subgroups { get; } = new List<Group>();
			public List<(MemberInfo, object)> Members { get; } = new List<(MemberInfo, object)>();

			public void AddSubgroup(Group group)
			{
				Subgroups.Add(group);
				group.Parent = this;
			}

			public bool UpdateProperties(ConfigGroupMemberAttribute groupAttribute, object container) => UpdateProperties(FromAttribute(groupAttribute, container));

			public bool UpdateProperties(Group reference)
			{
				LastInvalidUpdatePropertyName = null;

				Name ??= reference.Name;
				Id ??= reference.Id;
				LocalIndex ??= reference.LocalIndex;
				DefiningContainer ??= reference.DefiningContainer;

				if (Name != reference.Name && reference.Name != null)
					LastInvalidUpdatePropertyName = nameof(Name);
				if (Id != reference.Id && reference.Id != null)
					LastInvalidUpdatePropertyName = nameof(Id);
				if (LocalIndex != reference.LocalIndex)
					LastInvalidUpdatePropertyName = nameof(LocalIndex);
				if (DefiningContainer != reference.DefiningContainer && reference.DefiningContainer != null)
					LastInvalidUpdatePropertyName = nameof(DefiningContainer);

				return LastInvalidUpdatePropertyName == null;
			}

			public static Group FromAttribute(ConfigGroupMemberAttribute groupAttribute, object container)
			{
				return new Group() {
					Name = groupAttribute.GroupName,
					Id = groupAttribute.GroupId,
					LocalIndex = groupAttribute.GroupIndex,
					DefiningContainer = container
				};
			}
		}

		// TODO: make a Label UI wrapper (for TMPro.TextMeshProUGUI)
		public void Serialize()
		{
			Group baseGroup = new Group();

			foreach (var container in ConfigContainers)
			{
				Type type = container.GetType();
				PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				EventInfo[] events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public);
				// index -> (id, name)
				Dictionary<int, Group> localGroups = new Dictionary<int, Group>();
				List<(MemberInfo, object)> ungroupedMembers = new List<(MemberInfo, object)>();

				foreach (MemberInfo member in type.GetMembers())
				{
					ConfigProperty configProperty = member.GetCustomAttribute<ConfigProperty>();
					InvokableMethod invokableMethod = member.GetCustomAttribute<InvokableMethod>();
					if (configProperty == null && invokableMethod == null) continue;
					UpdateLocalGroups(member);
				}

				MergeGroups(localGroups, ungroupedMembers);

				void UpdateLocalGroups(MemberInfo member)
				{
					var groupAttribute = member.GetCustomAttribute<ConfigGroupMemberAttribute>();
					if (groupAttribute == null)
					{
						ungroupedMembers.Add((member, container));
						return;
					}

					if (localGroups.TryGetValue(groupAttribute.GroupIndex, out Group group))
					{
						if (!group.UpdateProperties(groupAttribute, container))
							Debug.LogWarning($"Group #{groupAttribute.GroupIndex} on {container} has several different {group.LastInvalidUpdatePropertyName}s!");
					}
					else
					{
						group = Group.FromAttribute(groupAttribute, container);
						localGroups.Add(groupAttribute.GroupIndex, group);
					}

					ValidateParent(group, groupAttribute);
					group.Members.Add((member, container));

					void ValidateParent(Group group, ConfigGroupMemberAttribute groupAttribute)
					{
						if (groupAttribute.ParentIndex < 0 && groupAttribute.ParentId == null) return;

						// ######################################## TODO: CHANGE localGroups to a global equivalent if needed #############################333333
						// ######################################## TODO: CHANGE localGroups to a global equivalent if needed #############################333333
						// ######################################## TODO: CHANGE localGroups to a global equivalent if needed #############################333333
						Group parent = groupAttribute.ParentId != null ?
							localGroups.Values.FirstOrDefault(x => x.Id == groupAttribute.ParentId) :
							(localGroups.ContainsKey(groupAttribute.ParentIndex) ? localGroups[groupAttribute.ParentIndex] : null);

						if (parent != null)
						{
							if (group.Parent == null) parent.AddSubgroup(group);
							if (parent == group.Parent) return;
							Debug.LogError($"Group {group.LocalIndex} on {group.DefiningContainer} has several different parents");
							return;
						}

						Group newParentGroup = new Group() { Id = groupAttribute.ParentId };
						if (groupAttribute.ParentIndex >= 0)
						{
							newParentGroup.LocalIndex = groupAttribute.ParentIndex;
							newParentGroup.DefiningContainer = container;
						}

						newParentGroup.AddSubgroup(group);
						localGroups.Add(groupAttribute.ParentIndex, newParentGroup);
					}
				}
			}

			foreach (Group group in baseGroup.Subgroups)
				SerializeTree(group, _uiParent);

			foreach (var member in baseGroup.Members)
				CreateControl(member.Item1, member.Item2, _uiParent);

			// TODO
			// Consider adding some kind of `reinitialize` function to VerticalUIStack,
			// so that it registers new children and adds MonoEvents objects to them
			_uiParent.GetComponent<VerticalUIStack>().RebuildLayout();

			void SerializeTree(Group group, RectTransform parent, float parentExtraIndent = 20)
			{
				if (group.Name != null)
				{
					GameObject newObject = Instantiate(_groupHeaderPrefab, parent);
					var label = newObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
					label.text = group.Name;
				}

				RectTransform groupTransform = CreateContainer(parent);
				groupTransform.offsetMin = new Vector2(parentExtraIndent + 20, groupTransform.offsetMin.y);

				foreach (var member in group.Members)
					CreateControl(member.Item1, member.Item2, groupTransform);

				foreach (Group subgroup in group.Subgroups)
					SerializeTree(subgroup, groupTransform, 0);

				AddVerticalStack(groupTransform.gameObject);
			}

			void MergeGroups(Dictionary<int, Group> localGroups, List<(MemberInfo, object)> ungroupedMembers)
			{
				foreach (Group group in localGroups.Values)
				{
					if (group.Parent != null) continue;

					Group idGroup = baseGroup.Subgroups.Find(x => x.Id == group.Id);
					if (group.Id == null || (group.Id != null && idGroup == null))
					{
						baseGroup.AddSubgroup(group);
						continue;
					}

					idGroup.Name ??= group.Name;
					if (idGroup.Name != group.Name && group.Name != null)
						Debug.LogError($"Conflicting group name definitions for group ID: {idGroup.Id}");

					foreach (Group subgroup in group.Subgroups)
						if (subgroup.Parent == null) idGroup.AddSubgroup(subgroup);

					idGroup.Members.AddRange(group.Members);
				}

				baseGroup.Members.AddRange(ungroupedMembers);
			}
		}

		private List<string> CreateControl(MemberInfo member, object container, RectTransform parent)
		{
			if (member is PropertyInfo property)
				return CreatePropertyControl(property, GetEvent(property), container, parent);

			if (member is MethodInfo method)
			{
				CreateMethodControl(method, container, parent);
				return new List<string>() { method.Name };
			}

			Debug.LogError($"Member {member} on {container} was not serialized - unknown member kind");
			return null;
		}

		private RectTransform CreateContainer(Transform parent, string name = null)
		{
			GameObject subContainer = new GameObject(name ?? "Container"); // Default game object name is longer...
			RectTransform transform = subContainer.AddComponent<RectTransform>();
			transform.SetParent(parent, false);
			transform.anchorMin = new Vector2(0, 1);
			transform.anchorMax = new Vector2(1, 1);
			transform.pivot = new Vector2(0.5f, 1);
			transform.offsetMin = new Vector2(0, transform.offsetMin.y);
			transform.offsetMax = new Vector2(0, transform.offsetMax.y);

			return transform;
		}

		private void AddVerticalStack(GameObject gameObject)
		{
			VerticalUIStack verticalStack = gameObject.AddComponent<VerticalUIStack>();
			verticalStack.BottomMargin = verticalStack.TopMargin = verticalStack.Spacing = 2;
		}

		private void CreateMethodControl(MethodInfo method, object container, RectTransform parent)
		{
			InvokableMethod invokableMethod = method.GetCustomAttribute<InvokableMethod>();
			GameObject newControl = Instantiate(_buttonPrefab, parent);
			Button button = newControl.GetComponent<Button>();
			button.TextLabel = invokableMethod.Name ?? SplitAndLowerCamelCase(method.Name);

			button.Click += () => method.Invoke(container, Array.Empty<object>());
		}

		private List<string> CreatePropertyControl(PropertyInfo property, EventInfo @event, object container, RectTransform parent) {
			Type type = property.PropertyType;

			if (type.IsEnum)
				return CreateDropdownList(property, @event, container, parent);
			if (IsIntegral(type) || type == typeof(float))
				return CreateSlider(property, @event, container, parent);
			else if (type == typeof(bool))
				return CreateToggle(property, @event, container, parent);
			else if (type == typeof(Color))
				return CreateColorButton(property, @event, container, parent);
			else if (type == typeof(Gradient))
				return CreateGradientButton(property, @event, container, parent);
			else if (type == typeof(AnimationCurve))
				return CreateCurveButton(property, @event, container, parent);
			else
				Debug.LogError($"Failed to create control for {property} on {container} : not implemented!");

			return null;
		}

		private List<string> CreateDropdownList(PropertyInfo property, EventInfo @event, object container, RectTransform parent)
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

			GameObject newControl = Instantiate(_dropdownPrefab, parent);
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

		private List<string> CreateCurveButton(PropertyInfo property, EventInfo @event, object container, RectTransform parent)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			CurvePickerButtonProperty curveProperty = configProperty as CurvePickerButtonProperty;
			Type configType = configProperty.GetType();

			if (curveProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			GameObject newControl = Instantiate(_curveButtonPrefab, parent);
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

		private List<string> CreateGradientButton(PropertyInfo property, EventInfo @event, object container, RectTransform parent)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			GradientPickerButtonProperty gradientProperty = configProperty as GradientPickerButtonProperty;
			Type configType = configProperty.GetType();

			if (gradientProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			GameObject newControl = Instantiate(_gradientButtonPrefab, parent);
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

		private List<string> CreateColorButton(PropertyInfo property, EventInfo @event, object container, RectTransform parent)
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			ColorPickerButtonProperty colorProperty = configProperty as ColorPickerButtonProperty;
			Type configType = configProperty.GetType();

			if (colorProperty == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			GameObject newControl = Instantiate(_colorButtonPrefab, parent);
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

		private List<string> CreateToggle(PropertyInfo property, EventInfo @event, object container, RectTransform parent)
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
				return CreateRadioButtons(property, @event, container, parent);

			GameObject newControl = Instantiate(_togglePrefab, parent);
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

		private List<string> CreateRadioButtons(PropertyInfo property, EventInfo @event, object container, RectTransform parent)
		{
			RadioButtonsProperty radioButtonsProperty = property.GetCustomAttribute<RadioButtonsProperty>();
			if (property.PropertyType == typeof(bool) && radioButtonsProperty.RadioNames.Length != 2)
				throw new ArgumentException("Can only create 2 radio buttons for bool property");

			RectTransform transform = CreateContainer(parent, $"{property.Name} radio group");
			var toggleGroup = transform.gameObject.AddComponent<UnityEngine.UI.ToggleGroup>();
			toggleGroup.allowSwitchOff = false;

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

			AddVerticalStack(transform.gameObject);

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
					radioButtons[Convert.ToInt32(x)].IsChecked = true;
					radioButtons[1 - Convert.ToInt32(x)].IsChecked = false;
				};

				@event.AddEventHandler(container, commonHandler);
			}

			return new List<string>() { property.Name };
		}

		private List<string> CreateSlider(PropertyInfo property, EventInfo @event, object container, RectTransform parent)
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
				return CreateMinMaxSlider(property, container, parent);

			GameObject newControl = Instantiate(_sliderPrefab, parent);
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

		private List<string> CreateMinMaxSlider(PropertyInfo property, object container, RectTransform parent)
		{
			MinMaxSliderProperty sliderProperty = property.GetCustomAttribute<MinMaxSliderProperty>();
			if (sliderProperty.HigherPropertyName == null) return null;

			PropertyInfo lower = property;
			PropertyInfo higher = property.DeclaringType.GetProperty(sliderProperty.HigherPropertyName);
			EventInfo lowerEvent = GetEvent(lower), higherEvent = GetEvent(higher);

			GameObject newControl = Instantiate(_minMaxSliderPrefab, parent);
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
		}

		private static EventInfo GetEvent(PropertyInfo property) => property.DeclaringType.GetEvent(property.Name + "Changed");

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
				FindObjectOfType<MainVisualizer>(),
				//FindObjectOfType<FragmentationVisualization>(),
				//FindObjectOfType<InteractionCore>(),
				//FindObjectOfType<ApplicationController>(),
				//FindObjectOfType<MiscConfigCollection>(),
			};
			Serialize();
		}
	}
}
