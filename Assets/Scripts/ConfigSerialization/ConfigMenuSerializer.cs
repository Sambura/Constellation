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

		private enum ControlType
		{
			Button, Toggle, GradientPickerButton, CurvePickerButton, ColorPickerButton,
			Slider, MinMaxSlider, RadioButtonArray, DropdownList, Container, GroupHeader
		}

		private class UINode
		{
			// RectTransform GameObject of this UI element
			public RectTransform Control { get; set; }
			// Main member that caused this UI element serizlization
			public MemberInfo Member { get; set; }
			// Instance containing the main member
			public object MemberContainer { get; set; }
			// Full list of member that contribute to this UI serialization (in cases when UI needs more than one member to exist, e.g. MinMaxSlider)
			public List<MemberInfo> SerializedMembers { get; set; }
			// Type of this UI element
			public ControlType Type { get; set; }
			// Parent of this UI element
			public UINode Parent { get; private set; }
			// Ordered list of children of the current node
			public List<UINode> Children { get; } = new List<UINode>();
			// Group to which this node belongs
			public Group Group { get; set; }
			// Additional data specific to control type
			public object Metadata { get; set; }

			public void SetParent(UINode parent)
			{
				parent.Children.Add(this);
				Parent = parent;
			}

			public UINode(UINode parent)
			{
				if (parent != null) SetParent(parent);
			}

			public UINode() { }
		}

		// TODO: make a Label UI wrapper (for TMPro.TextMeshProUGUI)
		// Btw this method is a total mess, pretty sure a lot of cross-file stuff will not work with this...
		public void Serialize()
		{
			Group baseGroup = new Group();
			var idGroups = new Dictionary<string, Group>();
			// group -> (toggle_property, invert_toggle)
			var groupToggles = new Dictionary<Group, (PropertyInfo, bool)>();
			var idGroupToggles = new Dictionary<string, (PropertyInfo, bool)>();
			UINode rootNode = new UINode() { Control = _uiParent };

			foreach (var container in ConfigContainers)
			{
				Type type = container.GetType();
				PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				EventInfo[] events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public);
				// index -> (id, name)
				Dictionary<int, Group> localGroups = new Dictionary<int, Group>();
				List<(MemberInfo, object)> ungroupedMembers = new List<(MemberInfo, object)>();
				var localToggles = new List<(PropertyInfo, ConfigGroupToggleAttribute)>();

				foreach (MemberInfo member in type.GetMembers())
				{
					ConfigProperty configProperty = member.GetCustomAttribute<ConfigProperty>();
					InvokableMethod invokableMethod = member.GetCustomAttribute<InvokableMethod>();
					if (configProperty == null && invokableMethod == null) continue;
					ConfigGroupToggleAttribute toggleAttribute = member.GetCustomAttribute<ConfigGroupToggleAttribute>();
					if (toggleAttribute != null)
					{
						if (member is PropertyInfo property)
							localToggles.Add((property, toggleAttribute));
						else
							Debug.LogError("Toggle attribute encountered on non-property member");
					}
					UpdateLocalGroups(member);
				}

				foreach (var toggleInfo in localToggles)
				{
					ConfigGroupToggleAttribute toggleParams = toggleInfo.Item2;

					if (toggleParams.GroupIndex != null)
						groupToggles.Add(localGroups[(int)toggleParams.GroupIndex], (toggleInfo.Item1, toggleParams.InvertToggle));
					else
						idGroupToggles.Add(toggleParams.GroupId, (toggleInfo.Item1, toggleParams.InvertToggle));

					if (toggleParams.InverseGroupIndex != null)
						groupToggles.Add(localGroups[(int)toggleParams.InverseGroupIndex], (toggleInfo.Item1, !toggleParams.InvertToggle));
					else if (toggleParams.InverseGroupId != null)
						idGroupToggles.Add(toggleParams.InverseGroupId, (toggleInfo.Item1, !toggleParams.InvertToggle));
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

			foreach (var toggleInfo in idGroupToggles)
				groupToggles.Add(idGroups[toggleInfo.Key], toggleInfo.Value);

			UINode rootContainer = CreateContainer(rootNode);

			foreach (Group group in baseGroup.Subgroups)
				SerializeTree(group, rootContainer);

			foreach (var member in baseGroup.Members)
				CreateControl(member.Item1, member.Item2, rootContainer);

			AddVerticalStack(rootContainer.Control.gameObject);

			List<UINode> uiNodes = UnwindUITree(rootNode);

			foreach (var toggleInfo in groupToggles)
			{
				Group targetGroup = toggleInfo.Key;
				UINode groupNode = uiNodes.Find(x => x.Type == ControlType.Container && x.Group == targetGroup);
				UINode toggleNode = uiNodes.Find(x => x.Member == toggleInfo.Value.Item1);
				BindToggleAndGroup(toggleInfo.Value.Item1, toggleNode, groupNode, toggleInfo.Value.Item2);
			}

			// TODO
			// Consider adding some kind of `reinitialize` function to VerticalUIStack,
			// so that it registers new children and adds MonoEvents objects to them
			_uiParent.GetComponent<VerticalUIStack>().RebuildLayout();

			List<UINode> UnwindUITree(UINode root, List<UINode> output = null)
			{
				var list = output ?? new List<UINode>();
				list.Add(root);

				foreach (UINode child in root.Children)
				{
					UnwindUITree(child, list);
				}

				return list;
			}

			void SerializeTree(Group group, UINode parent, float parentExtraIndent = 20)
			{
				if (group.Name != null)
				{
					GameObject newObject = Instantiate(_groupHeaderPrefab, parent.Control);
					var label = newObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
					label.text = group.Name;
				}

				UINode containerNode = CreateContainer(parent);
				containerNode.Group = group;
				RectTransform groupTransform = containerNode.Control;
				groupTransform.offsetMin = new Vector2(parentExtraIndent + 20, groupTransform.offsetMin.y);

				foreach (var member in group.Members)
					CreateControl(member.Item1, member.Item2, containerNode);

				foreach (Group subgroup in group.Subgroups)
					SerializeTree(subgroup, containerNode, 0);

				AddVerticalStack(groupTransform.gameObject);
			}

			void MergeGroups(Dictionary<int, Group> localGroups, List<(MemberInfo, object)> ungroupedMembers)
			{
				foreach (Group group in localGroups.Values)
				{
					if (group.Parent != null) continue;

					Group idGroup = (group.Id != null && idGroups.ContainsKey(group.Id)) ? idGroups[group.Id] : null;
					if (group.Id == null || (group.Id != null && idGroup == null))
					{
						baseGroup.AddSubgroup(group);
						if (group.Id != null) idGroups.Add(group.Id, group);
						continue;
					}

					idGroup.Name ??= group.Name;
					if (idGroup.Name != group.Name && group.Name != null)
						Debug.LogError($"Conflicting group name definitions for group ID: {idGroup.Id}");

					foreach (Group subgroup in group.Subgroups)
						if (subgroup.Parent == group) idGroup.AddSubgroup(subgroup);

					idGroup.Members.AddRange(group.Members);
				}

				baseGroup.Members.AddRange(ungroupedMembers);
			}
		}

		private void BindToggleAndGroup(PropertyInfo toggle, UINode toggleNode, UINode groupNode, bool invertToggle, bool repositionGroup = true)
		{
			RectTransform toggleTransform = toggleNode.Control;
			RectTransform groupTransform = groupNode.Control;

			GetEvent(toggle).AddEventHandler(toggleNode.MemberContainer, (Action<bool>)(x => {
				groupTransform.gameObject.SetActive(invertToggle ^ x);
			}));

			groupTransform.gameObject.SetActive(invertToggle ^ (bool)toggle.GetValue(toggleNode.MemberContainer));

			if (repositionGroup == false) return;


			if (toggleTransform.parent != groupTransform.parent)
			{
				Debug.LogWarning($"Could not bind toggle {toggle} and group {groupNode.Group}");
				return;
			}

			groupTransform.SetSiblingIndex(toggleTransform.GetSiblingIndex() + 1);
		}

		/// <summary>
		/// Create a UI control for the specified member, which belongs to the specified object, and 
		/// parents it to a specified UINode. Returns a newly created UINode.
		/// 
		/// It is basically a selector of CreatePropertyControl() vs. CreateMethodControl() methods
		/// </summary>
		private UINode CreateControl(MemberInfo member, object memberParent, UINode parent)
		{
			if (member is PropertyInfo property)
				return CreatePropertyControl(property, memberParent, parent);

			if (member is MethodInfo method)
				return CreateMethodControl(method, memberParent, parent);

			Debug.LogError($"Member {member} on {memberParent} was not serialized - unknown member kind");
			return null;
		}

		private UINode CreateMethodControl(MethodInfo method, object container, UINode parent)
		{
			InvokableMethod invokableMethod = method.GetCustomAttribute<InvokableMethod>();
			GameObject newControl = Instantiate(_buttonPrefab, parent.Control);
			Button button = newControl.GetComponent<Button>();
			button.TextLabel = invokableMethod.Name ?? SplitAndLowerCamelCase(method.Name);

			button.Click += () => method.Invoke(container, Array.Empty<object>());

			return new UINode(parent)
			{
				Type = ControlType.Button,
				Member = method,
				MemberContainer = container,
				Control = newControl.GetComponent<RectTransform>()
			};
		}

		private UINode CreatePropertyControl(PropertyInfo property, object container, UINode parent)
		{
			Type type = property.PropertyType;

			if (type.IsEnum)
				return CreateDropdownList(property, container, parent);
			if (IsIntegral(type) || type == typeof(float))
				return CreateSlider(property, container, parent);
			if (type == typeof(bool))
				return CreateToggle(property, container, parent);
			if (type == typeof(Color))
				return CreateColorButton(property, container, parent);
			if (type == typeof(Gradient))
				return CreateGradientButton(property, container, parent);
			if (type == typeof(AnimationCurve))
				return CreateCurveButton(property, container, parent);

			Debug.LogError($"Failed to create control for {property} on {container} : not implemented!");

			return null;
		}

		// Consider adding a property `DisplayOrder` to DropdownList class, in order to make native mapping support
		// If this is done, This method should be rewritable into a CreateUniversal wrapper
		private UINode CreateDropdownList(PropertyInfo property, object memberContainer, UINode parent)
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

			GameObject newControl = Instantiate(_dropdownPrefab, parent.Control);
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
			dropdown.SelectedValue = mapping.IndexOf((int)property.GetValue(memberContainer));

			Action<int> dropdownHandler = x => property.SetValue(memberContainer, mapping[x]);
			dropdown.SelectedValueChanged += dropdownHandler;

			if (configProperty.HasEvent)
			{
				MethodInfo getDelegate = GetType().GetMethod(nameof(GetDropdownContainerDelegate), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod(property.PropertyType);
				Delegate containerHandler = (Delegate)getDelegate.Invoke(null, new object[] { dropdown, mapping });

				GetEvent(property).AddEventHandler(memberContainer, containerHandler);
			}

			return new UINode(parent)
			{
				Control = newControl.GetComponent<RectTransform>(),
				Member = property,
				MemberContainer = memberContainer,
				Type = ControlType.DropdownList
			};
		}

		private static Delegate GetDropdownContainerDelegate<T>(DropdownList dropdown, List<int> mapping)
		{
			// I use IndexOf() to search for the needed mapping instead of creating an inverse mapping list
			// since this is a really rare operation I don't think it is worth to spend additional memory on it
			return (Action<T>)(x => dropdown.SelectedValue = mapping.IndexOf(Convert.ToInt32(x)));
		}

		private UINode CreateGradientButton(PropertyInfo property, object memberContainer, UINode parent)
		{
			return CreateUniversal<GradientPickerButtonProperty, GradientPickerButton>(
				property, _gradientButtonPrefab, memberContainer, parent, (x, y, z) =>
				{
					x.TextLabel = y.Name ?? SplitAndLowerCamelCase(property.Name);
					x.DialogTitle = z?.DialogTitle ?? $"Modify {SplitCamelCase(property.Name)}";
				},
				nameof(GradientPickerButton.Gradient), ControlType.GradientPickerButton
			);
		}

		private UINode CreateCurveButton(PropertyInfo property, object memberContainer, UINode parent)
		{
			return CreateUniversal<CurvePickerButtonProperty, CurvePickerButton>(
				property, _curveButtonPrefab, memberContainer, parent, (x, y, z) =>
				{
					x.TextLabel = y.Name ?? SplitAndLowerCamelCase(property.Name);
					x.DialogTitle = z?.DialogTitle ?? $"Modify {SplitCamelCase(property.Name)}";
				},
				nameof(CurvePickerButton.Curve), ControlType.CurvePickerButton
			);
		}

		private UINode CreateColorButton(PropertyInfo property, object memberContainer, UINode parent)
		{
			return CreateUniversal<ColorPickerButtonProperty, ColorPickerButton>(
				property, _colorButtonPrefab, memberContainer, parent, (x, y, z) =>
				{
					x.TextLabel = y.Name ?? SplitAndLowerCamelCase(property.Name);
					x.UseAlpha = z?.UseAlpha ?? true;
					x.DialogTitle = z?.DialogTitle ?? $"Select {SplitCamelCase(property.Name)}";
				},
				nameof(ColorPickerButton.Color), ControlType.ColorPickerButton
			);
		}

		private UINode CreateUniversal<T, V>(PropertyInfo property, GameObject prefab, object memberContainer, UINode parent,
				Action<V, ConfigProperty, T> initDelegate, string propertyName, ControlType controlType) 
			where T : ConfigProperty where V : Component
		{
			ConfigProperty configProperty = property.GetCustomAttribute<ConfigProperty>();
			T specificAttribute = configProperty as T;
			Type configType = configProperty.GetType();

			if (specificAttribute == null && configType != typeof(ConfigProperty))
			{
				Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
				return null;
			}

			GameObject newControl = Instantiate(prefab, parent.Control);
			V specificControl = newControl.GetComponent<V>();
			initDelegate(specificControl, configProperty, specificAttribute);
			var specificProperty = typeof(V).GetProperty(propertyName);
			specificProperty.SetValue(specificControl, property.GetValue(memberContainer));
			var controlEvent = GetEvent(specificProperty);
			Delegate handler = (Delegate)GetType().GetMethod(nameof(GetUniversalDelegate), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(property.PropertyType).Invoke(null, new object[] { property, specificProperty, memberContainer, specificControl });

			controlEvent.AddEventHandler(specificControl, handler);
			if (configProperty.HasEvent)
			{
				var parentEvent = GetEvent(property);
				if (parentEvent == null)
					Debug.LogError($"Event not found for property {property} on {memberContainer}");
				else
					parentEvent.AddEventHandler(memberContainer, handler);
			}

			return new UINode(parent)
			{
				Control = newControl.GetComponent<RectTransform>(),
				Member = property,
				MemberContainer = memberContainer,
				Type = controlType,
			};
		}

		private static Delegate GetUniversalDelegate<T>(PropertyInfo p1, PropertyInfo p2, object c1, object c2)
		{
			return (Action<T>)(x =>
			{
				p1.SetValue(c1, x);
				p2.SetValue(c2, x);
			});
		}

		private UINode CreateToggle(PropertyInfo property, object memberContainer, UINode parent)
		{
			RadioButtonsProperty radioButtonProperty = property.GetCustomAttribute<RadioButtonsProperty>();

			if (radioButtonProperty != null)
				return CreateRadioButtons(property, memberContainer, parent);

			return CreateUniversal<ConfigProperty, Toggle>(
				property, _togglePrefab, memberContainer, parent, (x, y, z) =>
				{ x.TextLabel = y.Name ?? SplitAndLowerCamelCase(property.Name); },
				nameof(Toggle.IsChecked), ControlType.Toggle
			);
		}

		private UINode CreateSlider(PropertyInfo property, object memberContainer, UINode parent)
		{
			if (property.GetCustomAttribute<MinMaxSliderProperty>() != null)
				return CreateMinMaxSlider(property, memberContainer, parent);

			bool isInt = IsIntegral(property.PropertyType);

			return CreateUniversal<SliderProperty, SliderWithText>(
				property, _sliderPrefab, memberContainer, parent, (x, y, z) =>
				{
					int intValue = isInt ? (int)property.GetValue(memberContainer) : 0;
					float floatValue = isInt ? intValue : (float)property.GetValue(memberContainer);

					x.MinValue = z?.MinValue ?? float.MinValue;
					x.MaxValue = z?.MaxValue ?? float.MaxValue;
					x.MinSliderValue = z?.MinSliderValue ?? (isInt ? intValue - 100 : floatValue - 5);
					x.MaxSliderValue = z?.MaxSliderValue ?? (isInt ? intValue + 100 : floatValue + 5);
					x.InputFormatting = z?.InputFormatting ?? (isInt ? "0" : "0.000");
					x.InputRegex = z?.InputRegex ?? @"([-+]?[0-9]*\.?[0-9]+)";
					x.RegexGroupIndex = z?.RegexGroupIndex ?? 1;
					x.TextLabel = y.Name ?? SplitAndLowerCamelCase(property.Name);
				},
				isInt ? nameof(SliderWithText.IntValue) : nameof(SliderWithText.Value), ControlType.Slider
			);
		}

		private UINode CreateRadioButtons(PropertyInfo property, object memberContainer, UINode parent)
		{
			RadioButtonsProperty radioButtonsProperty = property.GetCustomAttribute<RadioButtonsProperty>();
			if (property.PropertyType == typeof(bool) && radioButtonsProperty.RadioNames.Length != 2)
				throw new ArgumentException("Can only create 2 radio buttons for bool property");

			UINode containerNode = CreateContainer(parent, $"{property.Name} radio group");
			RectTransform transform = containerNode.Control;
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

			bool value = (bool)property.GetValue(memberContainer);

			radioButtons[Convert.ToInt32(value)].IsChecked = true;

			for (int i = 0; i < radioButtons.Count - 1; i++)
			{
				Action<bool> handler = (bool x) =>
				{
					property.SetValue(memberContainer, i == 0 ? x : !x);
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

				GetEvent(property).AddEventHandler(memberContainer, commonHandler);
			}

			return new UINode(parent)
			{
				Control = transform,
				Member = property,
				MemberContainer = memberContainer,
				Type = ControlType.RadioButtonArray,
				Metadata = radioButtons
			};
		}

		private UINode CreateMinMaxSlider(PropertyInfo property, object memberContainer, UINode parent)
		{
			MinMaxSliderProperty sliderProperty = property.GetCustomAttribute<MinMaxSliderProperty>();
			if (sliderProperty.HigherPropertyName == null) return null;

			PropertyInfo lower = property;
			PropertyInfo higher = property.DeclaringType.GetProperty(sliderProperty.HigherPropertyName);
			EventInfo lowerEvent = GetEvent(lower), higherEvent = GetEvent(higher);

			GameObject newControl = Instantiate(_minMaxSliderPrefab, parent.Control);
			MinMaxSliderWithInput slider = newControl.GetComponent<MinMaxSliderWithInput>();

			bool isInt = IsIntegral(lower.PropertyType);

			int lowerIntValue = isInt ? (int)lower.GetValue(memberContainer) : 0;
			float lowerFloatValue = isInt ? lowerIntValue : (float)lower.GetValue(memberContainer);
			int higherIntValue = isInt ? (int)higher.GetValue(memberContainer) : 0;
			float higherFloatValue = isInt ? higherIntValue : (float)higher.GetValue(memberContainer);

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
					lower.SetValue(memberContainer, x);
					slider.SetLowerValueWithoutNotify(x);
				};
				slider.IntLowerValueChanged += intLowerHandler;
				lowerEvent.AddEventHandler(memberContainer, intLowerHandler);

				Action<int> intHigherHandler = (int x) =>
				{
					higher.SetValue(memberContainer, x);
					slider.SetHigherValueWithoutNotify(x);
				};
				slider.IntHigherValueChanged += intHigherHandler;
				if (sliderProperty.HasEvent) higherEvent.AddEventHandler(memberContainer, intHigherHandler);
			}
			else
			{
				Action<float> floatLowerHandler = (float x) =>
				{
					lower.SetValue(memberContainer, x);
					slider.SetLowerValueWithoutNotify(x);
				};
				slider.LowerValueChanged += floatLowerHandler;
				lowerEvent.AddEventHandler(memberContainer, floatLowerHandler);

				Action<float> floatHigherHandler = (float x) =>
				{
					higher.SetValue(memberContainer, x);
					slider.SetHigherValueWithoutNotify(x);
				};
				slider.HigherValueChanged += floatHigherHandler;
				if (sliderProperty.HasEvent) higherEvent.AddEventHandler(memberContainer, floatHigherHandler);
			}

			return new UINode(parent)
			{
				Control = newControl.GetComponent<RectTransform>(),
				Member = property,
				MemberContainer = memberContainer,
				SerializedMembers = new List<MemberInfo>() { lower, higher },
				Type = ControlType.MinMaxSlider
			};
		}

		private UINode CreateContainer(UINode parent, string name = null)
		{
			GameObject container = new GameObject(name ?? "Container"); // Default game object name is longer...
			RectTransform transform = container.AddComponent<RectTransform>();
			transform.SetParent(parent.Control, false);
			transform.anchorMin = new Vector2(0, 1);
			transform.anchorMax = new Vector2(1, 1);
			transform.pivot = new Vector2(0.5f, 1);
			transform.offsetMin = new Vector2(0, transform.offsetMin.y);
			transform.offsetMax = new Vector2(0, transform.offsetMax.y);

			return new UINode(parent) { Control = transform, Type = ControlType.Container };
		}

		private void AddVerticalStack(GameObject gameObject)
		{
			VerticalUIStack verticalStack = gameObject.AddComponent<VerticalUIStack>();
			verticalStack.BottomMargin = verticalStack.TopMargin = verticalStack.Spacing = 2;
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
