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
		[Header("Prefabs")]
		[SerializeField] private GameObject _sliderPrefab;
		[SerializeField] private GameObject _togglePrefab;
		[SerializeField] private GameObject _colorButtonPrefab;
		[SerializeField] private GameObject _gradientButtonPrefab;
		[SerializeField] private GameObject _curveButtonPrefab;
		[SerializeField] private GameObject _minMaxSliderPrefab;
		[SerializeField] private GameObject _radioButtonPrefab;
		[SerializeField] private GameObject _buttonPrefab;
		[SerializeField] private GameObject _textureButtonPrefab;
		[SerializeField] private GameObject _dropdownPrefab;
		[SerializeField] private GameObject _groupHeaderPrefab;

		[Header("Tabs to generate")]
		[SerializeField] private List<ConfigTab> _tabs;

		[Header("Objects")]
		[SerializeField] private TabView _tabView;

		[Header("Parameters")]
		[SerializeField] private int _selectedTab;
		[SerializeField] private bool _serializeOnStart = false;

		[Serializable] private class ConfigTab
		{
			public string Name;
			public List<MonoBehaviour> ConfigSources;
		}

		private static readonly Type[] IntegralTypes = new[] { typeof(int), typeof(uint),
													   typeof(short), typeof(ushort),
													   typeof(long), typeof(ulong),
													   typeof(byte) };

		public static bool IsIntegral(Type type)
		{
			return IntegralTypes.Contains(type);
		}

		private class Orderable
		{
			public int? DisplayIndex { get; set; }
		}

		private class Member : Orderable
		{
			public MemberInfo MemberInfo { get; set; }
			public object MemberContainer { get; set; }
		}

		private class Group : Orderable
		{
			public string Name { get; set; }
			public string Id { get; set; }
			public int? LocalIndex { get; set; }
			public object DefiningContainer { get; set; }
			public Group Parent { get; set; }
			public string LastInvalidUpdatePropertyName { get; private set; }

			public List<Group> Subgroups { get; } = new List<Group>();
			public List<Member> Members { get; } = new List<Member>();

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
				DisplayIndex ??= reference.DisplayIndex;

				if (Name != reference.Name && reference.Name != null)
					LastInvalidUpdatePropertyName = nameof(Name);
				if (Id != reference.Id && reference.Id != null)
					LastInvalidUpdatePropertyName = nameof(Id);
				if (LocalIndex != reference.LocalIndex)
					LastInvalidUpdatePropertyName = nameof(LocalIndex);
				if (DisplayIndex != reference.DisplayIndex)
					LastInvalidUpdatePropertyName = nameof(DisplayIndex);
				if (DefiningContainer != reference.DefiningContainer && reference.DefiningContainer != null)
					LastInvalidUpdatePropertyName = nameof(DefiningContainer);

				return LastInvalidUpdatePropertyName == null;
			}

			public static Group FromAttribute(ConfigGroupMemberAttribute groupAttribute, object container)
			{
				return new Group()
				{
					Name = groupAttribute.GroupName,
					Id = groupAttribute.GroupId,
					LocalIndex = groupAttribute.GroupIndex,
					DefiningContainer = container,
					DisplayIndex = groupAttribute.DisplayIndex
				};
			}
		}

		private enum ControlType
		{
			Button, Toggle, GradientPickerButton, CurvePickerButton, ColorPickerButton,
			Slider, MinMaxSlider, RadioButtonArray, DropdownList, Container, GroupHeader,
			TexturePickerButton
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

		public void Serialize()
		{
			// Global dictionary of groups that have IDs
			var idGroups = new Dictionary<string, Group>();
			// group -> (toggle_property, invert_toggle)
			var groupToggles = new Dictionary<Group, (PropertyInfo, bool)>();
			// UINode without a real GameObject behind it. Used to collect all the tab's UINodes as children
			UINode rootNode = new UINode();

			foreach (ConfigTab tab in _tabs)
			{
				var idGroupToggles = new Dictionary<string, (PropertyInfo, bool)>();
				// Pseudo group used to collect all the top-level groups and members as its children
				Group baseGroup = new Group();

				RectTransform tabTransform = _tabView.AddTab(tab.Name);
				UINode tabNode = new UINode() { Control = tabTransform };
				tabNode.SetParent(rootNode);

				foreach (var container in tab.ConfigSources)
				{
					Type type = container.GetType();
					PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
					EventInfo[] events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public);
					// index -> (id, name)
					Dictionary<int, Group> localGroups = new Dictionary<int, Group>();
					List<Member> ungroupedMembers = new List<Member>();
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

					MergeGroups(baseGroup, localGroups, ungroupedMembers);

					void UpdateLocalGroups(MemberInfo member)
					{
						var groupAttribute = member.GetCustomAttribute<ConfigGroupMemberAttribute>();
						var orderAttribute = member.GetCustomAttribute<ConfigMemberOrderAttribute>();
						Member newMember = new Member { MemberInfo = member, MemberContainer = container, DisplayIndex = orderAttribute?.DisplayIndex };

						if (groupAttribute == null)
						{
							ungroupedMembers.Add(newMember);
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
						group.Members.Add(newMember);

						void ValidateParent(Group group, ConfigGroupMemberAttribute groupAttribute)
						{
							if (groupAttribute.ParentIndex < 0 && groupAttribute.ParentId == null) return;

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

				FinilizeContainer(baseGroup, tabNode, tabTransform);
			}

			// Binding toggles to groups

			List<UINode> uiNodes = UnwindUITree(rootNode);

			foreach (var toggleInfo in groupToggles)
			{
				Group targetGroup = toggleInfo.Key;
				UINode groupNode = uiNodes.Find(x => x.Type == ControlType.Container && x.Group == targetGroup);
				UINode toggleNode = uiNodes.Find(x => x.Member == toggleInfo.Value.Item1);
				BindToggleAndGroup(toggleInfo.Value.Item1, toggleNode, groupNode, toggleInfo.Value.Item2);
			}

			_tabView.SelectTab(_selectedTab);


			// ##################################################################
			// ##################################################################
			// ##################################################################


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
					LabeledUIElement label = newObject.GetComponent<LabeledUIElement>();
					label.LabelText = group.Name;
				}

				UINode containerNode = CreateContainer(parent);
				containerNode.Group = group;
				RectTransform groupTransform = containerNode.Control;
				groupTransform.offsetMin = new Vector2(parentExtraIndent + 20, groupTransform.offsetMin.y);

				FinilizeContainer(group, containerNode, groupTransform, 0);
			}

			void FinilizeContainer(Group group, UINode containerNode, RectTransform groupTransform, float parentExtraIndent = 20)
			{
				List<Orderable> orderedList = new List<Orderable>();
				List<Orderable> unorderedObjects = new List<Orderable>();

				while (orderedList.Count < group.Subgroups.Count + group.Members.Count) orderedList.Add(null);

				foreach (Orderable orderable in group.Members.Cast<Orderable>().Concat(group.Subgroups))
				{
					if (orderable.DisplayIndex != null)
					{
						int value = orderable.DisplayIndex.Value;
						int actualIndex = value < 0 ? value + orderedList.Count : value;
						if (orderedList[actualIndex] != null)
						{
							orderedList.RemoveAt(orderedList.Count - 1);
						}
						orderedList[actualIndex] = orderable;
						continue;
					}

					unorderedObjects.Add(orderable);
				}

				int index = 0;
				foreach (Orderable orderable in unorderedObjects)
				{
					while (orderedList[index] != null) index++;
					orderedList[index] = orderable;
				}

				foreach (Orderable orderable in orderedList)
				{
					if (orderable is Member member) CreateControl(member.MemberInfo, member.MemberContainer, containerNode);
					else if (orderable is Group subgroup) SerializeTree(subgroup, containerNode, parentExtraIndent);
					else Debug.LogError("Somethings wrong I can feel it");
				}

				AddVerticalStack(groupTransform.gameObject);
			}

			void MergeGroups(Group baseGroup, Dictionary<int, Group> localGroups, List<Member> ungroupedMembers)
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

			GetEvent(toggle).AddEventHandler(toggleNode.MemberContainer, (Action<bool>)(x =>
			{
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
			button.LabelText = invokableMethod.Name ?? SplitAndLowerCamelCase(method.Name);

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
			if (type == typeof(Texture2D))
				return CreateTextureButton(property, container, parent);

			Debug.LogError($"Failed to create control for {property} on {container} : not implemented!");

			return null;
		}

		private UINode CreateGradientButton(PropertyInfo property, object memberContainer, UINode parent)
		{
			return CreateUniversal<GradientPickerButtonProperty, GradientPickerButton>(
				property, _gradientButtonPrefab, memberContainer, parent, (x, y, z) =>
				{
					x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
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
					x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
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
					x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
					x.UseAlpha = z?.UseAlpha ?? true;
					x.DialogTitle = z?.DialogTitle ?? $"Select {SplitCamelCase(property.Name)}";
				},
				nameof(ColorPickerButton.Color), ControlType.ColorPickerButton
			);
		}

		private UINode CreateTextureButton(PropertyInfo property, object memberContainer, UINode parent)
		{
			return CreateUniversal<ConfigProperty, TexturePickerButton>(
				property, _textureButtonPrefab, memberContainer, parent, (x, y, z) =>
				{
					x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
					x.DialogTitle = $"Select {SplitCamelCase(property.Name)}";
				},
				nameof(TexturePickerButton.Texture), ControlType.TexturePickerButton
			);
		}

		private UINode CreateDropdownList(PropertyInfo property, object memberContainer, UINode parent)
		{
			if (!property.PropertyType.IsEnum) throw new NotImplementedException("Currently only enums properties can have dropdown list");

			return CreateUniversal<DropdownListProperty, DropdownList>(
				property, _dropdownPrefab, memberContainer, parent, (x, y, z) =>
				{
					x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
					string[] names = z.DisplayedOptions == null ? null : new string[z.DisplayedOptions.Length];
					for (int i = 0; i < (z.DisplayedOptions?.Length ?? 0); i++) names[i] = SplitAndLowerCamelCase(z.DisplayedOptions[i].ToString());
					x.SetOptions(new List<string>(z.OptionNames ?? names ?? property.PropertyType.GetEnumNames()));
				},
				nameof(DropdownList.SelectedValue), ControlType.DropdownList, (x, z) =>
				{
					List<int> mapping = new List<int>();
					for (int i = 0; i < x.Options.Count; i++) mapping.Add(i);

					for (int i = 0; i < (z.DisplayedOptions?.Length ?? 0); i++)
					{
						if (z.DisplayedOptions[i].GetType() != property.PropertyType)
							throw new ArgumentException("Dropdown displayed options contain invalid types");
						mapping[i] = Convert.ToInt32(z.DisplayedOptions[i]);
					}

					return ((Delegate, Delegate))GetType().GetMethod(nameof(GenerateDropdownListConverters), BindingFlags.Static | BindingFlags.NonPublic)
							.MakeGenericMethod(property.PropertyType).Invoke(null, new object[] { mapping });
				}
			);
		}

		private static (Delegate, Delegate) GenerateDropdownListConverters<T>(List<int> mapping) where T : Enum
		{
			return (
				(Func<T, int>)(x => mapping.IndexOf(Convert.ToInt32(x))),
				(Func<int, T>)(x => (T)Enum.ToObject(typeof(T), mapping[x]))
			);
		}

		/// <summary>
		/// converterGenerator should return (prop2ui, ui2prop)
		/// </summary>
		private UINode CreateUniversal<T, V>(PropertyInfo property, GameObject prefab, object memberContainer,
			UINode parent, Action<V, ConfigProperty, T> initDelegate, string propertyName, ControlType controlType,
			Func<V, T, (Delegate, Delegate)> converterGenerator = null) where T : ConfigProperty where V : Component
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
			var controlEvent = GetEvent(specificProperty);
			Delegate handler = (Delegate)GetType().GetMethod(nameof(GetUniversalHandler), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(property.PropertyType).Invoke(null, new object[] { property, specificProperty, memberContainer, specificControl });
			Delegate propToUiHandler = handler, uiToPropHandler = handler;
			if (converterGenerator != null)
			{
				(Delegate prop2ui, Delegate ui2prop) = converterGenerator(specificControl, specificAttribute);
				propToUiHandler = (Delegate)GetType().GetMethod(nameof(GetDirectionalHandler), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(property.PropertyType, specificProperty.PropertyType).Invoke(null, new object[] { specificProperty, specificControl, prop2ui });
				uiToPropHandler = (Delegate)GetType().GetMethod(nameof(GetDirectionalHandler), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(specificProperty.PropertyType, property.PropertyType).Invoke(null, new object[] { property, memberContainer, ui2prop });

				specificProperty.SetValue(specificControl, prop2ui.DynamicInvoke(property.GetValue(memberContainer)));
			} else
			{
				specificProperty.SetValue(specificControl, property.GetValue(memberContainer));
			}

			controlEvent.AddEventHandler(specificControl, uiToPropHandler);
			if (configProperty.HasEvent)
			{
				var parentEvent = GetEvent(property);
				if (parentEvent == null)
					Debug.LogError($"Event not found for property {property} on {memberContainer}");
				else
					parentEvent.AddEventHandler(memberContainer, propToUiHandler);
			}

			return new UINode(parent)
			{
				Control = newControl.GetComponent<RectTransform>(),
				Member = property,
				MemberContainer = memberContainer,
				Type = controlType,
			};
		}

		private static Delegate GetUniversalHandler<T>(PropertyInfo prop1, PropertyInfo prop2, object cont1, object cont2)
		{
			return (Action<T>)(x =>
			{
				prop1.SetValue(cont1, x);
				prop2.SetValue(cont2, x);
			});
		}

		/// <summary>
		/// property is a Property that is going to be updated by this handler
		/// property has type S, whereas input is type T
		/// </summary>
		private static Delegate GetDirectionalHandler<T, S>(PropertyInfo property, object container, Delegate converter)
		{
			Func<T, S> t2o = (Func<T, S>)converter;
			return (Action<T>)(x => property.SetValue(container, t2o(x)));
		}

		private UINode CreateToggle(PropertyInfo property, object memberContainer, UINode parent)
		{
			RadioButtonsProperty radioButtonProperty = property.GetCustomAttribute<RadioButtonsProperty>();

			if (radioButtonProperty != null)
				return CreateRadioButtons(property, memberContainer, parent);

			return CreateUniversal<ConfigProperty, Toggle>(
				property, _togglePrefab, memberContainer, parent, (x, y, z) =>
				{ x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name); },
				nameof(Toggle.IsChecked), ControlType.Toggle
			);
		}

		private UINode CreateSlider(PropertyInfo property, object memberContainer, UINode parent)
		{
			if (property.GetCustomAttribute<MinMaxSliderProperty>() != null)
				return CreateMinMaxSlider(property, memberContainer, parent);

			bool isInt = IsIntegral(property.PropertyType);

			return CreateUniversal<SliderProperty, Slider>(
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
					x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
				},
				isInt ? nameof(Slider.IntValue) : nameof(Slider.Value), ControlType.Slider
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
				toggle.LabelText = name;
				toggle.ToggleGroup = toggleGroup;
			}

			AddVerticalStack(transform.gameObject);

			bool value = (bool)property.GetValue(memberContainer);

			radioButtons[Convert.ToInt32(value)].IsChecked = true;

			for (int i = 0; i < radioButtons.Count - 1; i++)
				radioButtons[i].IsCheckedChanged += x => property.SetValue(memberContainer, i == 0 ? x : !x);

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
			slider.LabelText = sliderProperty.Name ?? SplitAndLowerCamelCase(lower.Name);
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

		private VerticalUIStack AddVerticalStack(GameObject gameObject)
		{
			VerticalUIStack verticalStack = gameObject.AddComponent<VerticalUIStack>();
			verticalStack.BottomMargin = verticalStack.TopMargin = verticalStack.Spacing = 2;
			return verticalStack;
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

		private void Start() { if (_serializeOnStart) Serialize(); }
	}
}
