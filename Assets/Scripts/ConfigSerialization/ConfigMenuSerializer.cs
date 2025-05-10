using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ConstellationUI;
using ConfigSerialization.Structuring;
using static Core.Utility;
using Core;

namespace ConfigSerialization
{
    public class ConfigMenuSerializer : MonoBehaviour
    {
        [Header("Property control prefabs")]
        [SerializeField] private GameObject _sliderPrefab;
        [SerializeField] private GameObject _togglePrefab;
        [SerializeField] private GameObject _minMaxSliderPrefab;
        [SerializeField] private GameObject _colorButtonPrefab;
        [SerializeField] private GameObject _gradientButtonPrefab;
        [SerializeField] private GameObject _curveButtonPrefab;
        [SerializeField] private GameObject _radioButtonPrefab;
        [SerializeField] private GameObject _dropdownPrefab;
        [SerializeField] private GameObject _textureButtonPrefab;
        [SerializeField] private GameObject _numericInputFieldPrefab;
        [SerializeField] private GameObject _textInputFieldPrefab;
        [SerializeField] private GameObject _filePathSelectorPrefab;
        [SerializeField] private GameObject _unboundedListViewPrefab;

        [Header("No-input property control prefabs")]
        [SerializeField] private GameObject _colorIndicatorPrefab;
        [SerializeField] private GameObject _labelPrefab;

        [Header("Other prefabs")]
        [SerializeField] private GameObject _collapsableHeaderPrefab;
        [SerializeField] private GameObject _groupHeaderPrefab;
        [SerializeField] private GameObject _buttonPrefab;

        [Header("Tabs to generate")]
        [SerializeField] private List<ConfigTab> _tabs;

        [Header("Objects")]
        [SerializeField] private TabView _tabView;

        [Header("Parameters")]
        [SerializeField] private int _selectedTab;
        [SerializeField] private bool _serializeOnStart = false;
        [SerializeField] private float _pollFrequency = 15;

        private readonly Dictionary<Func<Type, bool>, Func<PropertyInfo, object, UINode, UINode>> _propertyControlCreators;
        private readonly Dictionary<Func<Type, bool>, Func<PropertyInfo, object, UINode, UINode>> _readonlyPropertyControlCreators;

        public static ConfigMenuSerializer MainInstance { get; private set; }

        /// <summary>
        /// Configuration for a menu tab to generate. Contains name and the list of scripts 
        /// containing the ConfigProperty-ies to serialize to the tab
        /// </summary>
        [Serializable] private class ConfigTab
        {
            public string Name;
            public List<MonoBehaviour> ConfigSources;
        }

        /// <summary>
        /// Simplest member of UI tree, only contains information about its relative order under the parent node
        /// </summary>
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
            /// <summary>
            /// Displayed group title
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Group unique id
            /// </summary>
            public string Id { get; set; }
            /// <summary>
            /// Group index. This index is local to the group's defining container (class)
            /// </summary>
            public int? LocalIndex { get; set; }
            /// <summary>
            /// The object that defined this group. Typically a MonoBehaviour script
            /// </summary>
            public object DefiningContainer { get; set; }
            /// <summary>
            /// Rendering layout mode
            /// </summary>
            public ConfigGroupLayout Layout { get; set; } = ConfigGroupLayout.Default;
            /// <summary>
            /// Should group be indented when rendering?
            /// </summary>
            public bool? Indent { get; set; }
            /// <summary>
            /// Parent group
            /// </summary>
            public Group Parent { get; set; }
            /// <summary>
            /// Needed internally for error messages
            /// </summary>
            public string LastInvalidUpdatePropertyName { get; private set; }

            /// <summary>
            /// Children groups
            /// </summary>
            public List<Group> Subgroups { get; } = new List<Group>();
            /// <summary>
            /// Children members. Each member corresponds to a UI control bound to a ConfigProperty
            /// </summary>
            public List<Member> Members { get; } = new List<Member>();

            public bool GetIndent() => Indent ?? true;

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
                if (Layout == ConfigGroupLayout.Default) Layout = reference.Layout;
                Indent ??= reference.Indent;

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
                if (Layout != reference.Layout && reference.Layout != ConfigGroupLayout.Default)
                    LastInvalidUpdatePropertyName = nameof(Layout);
                if (Indent != reference.Indent && reference.Indent is { })
                    LastInvalidUpdatePropertyName = nameof(Indent);

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
                    DisplayIndex = groupAttribute.DisplayIndex,
                    Layout = groupAttribute.Layout,
                    Indent = groupAttribute.Indent
                };
            }
        }

        private enum ControlType
        {
            Unknown, Button, Toggle, GradientPickerButton, CurvePickerButton, ColorPickerButton,
            Slider, MinMaxSlider, RadioButtonArray, DropdownList, Container, GroupHeader,
            TexturePickerButton, ColorIndicator, NumericInputField, FilePathSelector,
            TextInputField, NullableControl, OutputLabel, UnboundedListView
        }

        private class UINode
        {
            // RectTransform GameObject of this UI element
            public RectTransform Control { get; set; }
            // Main member that caused this UI element serialization
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

        /// <summary>
        /// The main function. Analyzes tab config and generates the UI for all the defined ConfigProperty-ies
        /// </summary>
        public int GenerateMenuUI(RectTransform container = null, object propertySource = null)
        {
            // Global dictionary of groups that have IDs
            var idGroups = new Dictionary<string, Group>();
            // Property -> (resolved bound groups, unresolved group ids)
            var groupToggles = new Dictionary<PropertyInfo, (List<Group>[], List<string>[])>();
            // Groups already bound to some property
            var boundGroups = new Dictionary<Group, PropertyInfo>();
            // UINode without a real GameObject behind it. Used to collect all the tab's UINodes as children
            UINode rootNode = new UINode() { Control = container };
            int serializedCount = 0;

            // UI generation
            if (propertySource is null)
            {
                foreach (ConfigTab tab in _tabs)
                {
                    // Pseudo group used to collect all the top-level groups and members as its children
                    Group baseGroup = new Group();

                    RectTransform tabTransform = _tabView.AddTab(tab.Name);
                    UINode tabNode = new UINode() { Control = tabTransform };
                    tabNode.SetParent(rootNode);

                    PrepareTab(tabNode, baseGroup, tab.ConfigSources.Cast<object>().ToList());
                }
            } else {
                Group baseGroup = new Group();
                PrepareTab(rootNode, baseGroup, new List<object>() { propertySource });
            }

            // Resolve toggle bindings
            var resolvedGroupToggles = new Dictionary<PropertyInfo, List<Group>[]>();
            foreach (var toggleBinding in groupToggles)
            {
                PropertyInfo property = toggleBinding.Key;
                List<Group>[] bindingMap = toggleBinding.Value.Item1;
                List<string>[] unresolvedIds = toggleBinding.Value.Item2;
                for (int i = 0; i < bindingMap.Length; i++)
                {
                    for (int j = 0; j < bindingMap[i].Count; j++)
                    {
                        if (boundGroups.TryGetValue(bindingMap[i][j], out PropertyInfo masterProperty) && masterProperty != property)
                        {
                            Debug.LogWarning($"Failed to resolve toggle binding for {property}: group already bound to {boundGroups[bindingMap[i][j]]}");
                            bindingMap[i].RemoveAt(j--);
                        }
                    }

                    for (int j = 0; j < unresolvedIds[i].Count; j++)
                    {
                        if (idGroups.TryGetValue(unresolvedIds[i][j], out Group resolvedGroup))
                            bindingMap[i].Add(resolvedGroup); 
                        else 
                            Debug.LogWarning($"Failed to resolve toggle binding for {property}: could not find group `{unresolvedIds[i][j]}`");
                    }

                    for (int j = 0; j < unresolvedIds[i].Count; j++)
                    {
                        if (!boundGroups.ContainsKey(bindingMap[i][j])) boundGroups.Add(bindingMap[i][j], property);
                    }
                }

                resolvedGroupToggles.Add(property, bindingMap);
            }

            List<UINode> uiNodes = UnwindUITree(rootNode);

            // Binding toggles to groups
            foreach (var toggleInfo in resolvedGroupToggles)
            {
                UINode toggleNode = uiNodes.Find(x => x.Member == toggleInfo.Key);
                RectTransform lastControl = toggleNode.Control;
                List<Transform>[] transforms = new List<Transform>[toggleInfo.Value.Length];
                bool reorder = !toggleInfo.Key.GetCustomAttribute<ConfigGroupToggleAttribute>().DoNotReorder;

                for (int i = 0; i < transforms.Length; i++)
                {
                    List<Group> targetGroups = toggleInfo.Value[i];
                    transforms[i] = new List<Transform>();

                    for (int j = 0; j < targetGroups.Count; j++)
                    {
                        Group targetGroup = targetGroups[j];
                        UINode groupNode = uiNodes.Find(x => x.Type == ControlType.Container && x.Group == targetGroup);

                        if (reorder) PlaceAfterInHierarchy(lastControl, groupNode.Control);
                        lastControl = groupNode.Control;
                        transforms[i].Add(lastControl);
                    }
                }

                BindPropertyToObjects(toggleInfo.Key, toggleNode.MemberContainer, transforms);
            }

            return serializedCount;

            // ##################################################################
            // ###################       Local functions	 ####################
            // ##################################################################

            void PrepareTab(UINode node, Group group, List<object> sources)
            {
                foreach (var container in sources)
                {
                    Type type = container.GetType();
                    // index -> (id, name)
                    Dictionary<int, Group> localGroups = new Dictionary<int, Group>();
                    List<Member> ungroupedMembers = new List<Member>();
                    var localToggles = new List<(PropertyInfo, ConfigGroupToggleAttribute)>();

                    foreach (MemberInfo member in type.GetMembers())
                    {
                        ConfigProperty configProperty = member.GetCustomAttribute<ConfigProperty>();
                        InvokableMethod invokableMethod = member.GetCustomAttribute<InvokableMethod>();
                        if (configProperty == null && invokableMethod == null) continue;
                        serializedCount++;
                        ConfigGroupToggleAttribute toggleAttribute = member.GetCustomAttribute<ConfigGroupToggleAttribute>();
                        if (toggleAttribute is { })
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
                        object[] rawBindingMap = toggleParams.Mapping;
                        List<string>[] groupIds = new List<string>[rawBindingMap.Length];
                        List<Group>[] toggledGroups = new List<Group>[rawBindingMap.Length];

                        for (int i = 0; i < rawBindingMap.Length; i++)
                        {
                            groupIds[i] = new List<string>(); toggledGroups[i] = new List<Group>();

                            ProcessBinding(rawBindingMap[i]);

                            void ProcessBinding(object entry)
                            {
                                if (entry is null) return;
                                if (entry.GetType() == typeof(int))
                                {
                                    if (!localGroups.TryGetValue((int)entry, out Group toggledGroup))
                                        Debug.LogWarning($"Failed to resolve toggle binding for {toggleInfo.Item1}: group index {(int)entry} not found");
                                    toggledGroups[i].Add(toggledGroup);
                                }
                                else if (entry.GetType() == typeof(string))
                                {
                                    groupIds[i].Add((string)entry);
                                }
                                else if (entry.GetType().IsArray)
                                {
                                    Array array = (Array)entry;
                                    for (int j = 0; j < array.Length; j++) ProcessBinding(array.GetValue(j));
                                }
                                else
                                {
                                    Debug.LogWarning($"Failed to resolve toggle binding for {toggleInfo.Item1}: binding {i} has unsupported " +
                                        $"type ({entry.GetType()}). The binding type should be null, int, string, or an array of these types");
                                }
                            }
                        }

                        groupToggles.Add(toggleInfo.Item1, (toggledGroups, groupIds));
                    }

                    MergeGroups(group, localGroups, ungroupedMembers);

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

                FinalizeContainer(group, node, node.Control);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(node.Control);
            }

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
                UINode node = CreateGroupContainer(group, parent, parentExtraIndent + (group.GetIndent() ? 20 : 0));

                FinalizeContainer(group, node, node.Control, 0);
            }

            void FinalizeContainer(Group group, UINode containerNode, RectTransform groupTransform, float parentExtraIndent = 20)
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
                    if (orderable is Member member) DecorateControl(CreateControl(member.MemberInfo, member.MemberContainer, containerNode));
                    else if (orderable is Group subgroup) SerializeTree(subgroup, containerNode, parentExtraIndent);
                    else Debug.LogError("Somethings wrong I can feel it");
                }

                ApplyLayout(groupTransform.gameObject, group.Layout);
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
        } // End of GenerateMenuUI

        private void IndentContainer(UINode container, float indent)
        {
            RectTransform groupTransform = container.Control;
            groupTransform.offsetMin = new Vector2(indent, groupTransform.offsetMin.y);
        }

        private UINode CreateGroupContainer(Group group, UINode parent, float indent, bool collapsable = true)
        {
            GameObject header = null;

            if (group.Name != null)
            {
                header = Instantiate(collapsable ? _collapsableHeaderPrefab : _groupHeaderPrefab, parent.Control);
                LabeledUIElement label = header.GetComponent<LabeledUIElement>();
                label.LabelText = group.Name;
            }

            UINode containerNode = CreateContainer(parent);
            IndentContainer(containerNode, indent);
            containerNode.Group = group;

            if (header is { } && collapsable)
                BindToggleToObject(header, containerNode.Control, false);

            return containerNode;
        }

        private void BindToggleToObject(GameObject toggleObject, Transform gameObject, bool invertToggle = false)
        {
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            if (toggle is null) throw new ArgumentException("Provided toggleNode does not contain a toggle Component");

            PropertyInfo isChecked = typeof(Toggle).GetProperty(nameof(Toggle.IsChecked));
            Transform[] binding = invertToggle ? new Transform[] { null, gameObject } : new Transform[] { gameObject };
            BindPropertyToObjects(isChecked, toggle, binding);
        }

        /// <summary>
        /// Binds a bool/Enum property to multiple objects. For more information on idea of binding group to properties, see 
        /// <see cref="ConfigGroupToggleAttribute"/>
        /// </summary>
        /// <param name="property">Property to bind to an object. Should have a corresponding PropertyChanged event in defining class</param>
        /// <param name="propertyObject">Object that defines `property` and its PropertyChanged event</param>
        /// <param name="gameObjects">Transforms of the objects to bind to the property. There should be at least 1 element in the array</param>
        private void BindPropertyToObjects(PropertyInfo property, object propertyObject, List<Transform>[] gameObjects)
        {
            EventInfo toggleEvent = GetEvent(property);
            if (toggleEvent is null)
            {
                Debug.LogError($"Failed to bind {property} value to objects: event not found");
                return;
            }

            object initialValue = property.GetValue(propertyObject);
            Type propertyType = property.PropertyType;
            Func<object, int> indexer = propertyType == typeof(bool) ? BoolToIndex :
                                        propertyType == typeof(int) ? IntToIndex :
                                        propertyType.IsEnum ? (Func<object, int>)EnumToIndex : null;

            if (indexer is null) {
                Debug.LogWarning($"Cannot bind property {property} to object : type {propertyType} not supported");
                return;
            }

            Type genericBinderType = typeof(PTOBinder<>).MakeGenericType(propertyType);
            object binder = Activator.CreateInstance(genericBinderType, gameObjects, indexer);
            MethodInfo binderInfo = genericBinderType.GetMethod(nameof(PTOBinder<int>.EventHandler));
            Delegate binderMethod = binderInfo.CreateDelegate(typeof(Action<>).MakeGenericType(propertyType), binder);
            toggleEvent.AddEventHandler(propertyObject, binderMethod);
            binderMethod.DynamicInvoke(initialValue);

            int BoolToIndex(object value) => (bool)value ? 0 : 1;

            int IntToIndex(object value) => (int)value;

            int EnumToIndex(object value)
            {
                Array enumValues = propertyType.GetEnumValues();
                for (int i = 0; i < enumValues.Length; i++)
                    if (enumValues.GetValue(i).Equals(value)) return i;

                return -1;
            }
        }

        // Same but only 1 object per property value, for convinience
        private void BindPropertyToObjects(PropertyInfo property, object propertyObject, Transform[] gameObjects)
        {
            List<Transform>[] wrappedObjects = new List<Transform>[gameObjects.Length];
            for (int i = 0; i < wrappedObjects.Length; i++) {
                wrappedObjects[i] = new List<Transform>() { gameObjects[i] };
            }

            BindPropertyToObjects(property, propertyObject, wrappedObjects);
        }

        /// <summary>
        /// Have not figured out an easier way to make an event handler for dynamically resolved enum types
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        private class PTOBinder<T>
        {
            public List<Transform>[] GameObjects;
            public Func<object, int> Indexer;

            public void EventHandler(T newValue)
            {
                foreach (List<Transform> transforms in GameObjects)
                    foreach (Transform transform in transforms)
                        transform?.gameObject.SetActive(false);

                int objectIndex = Indexer.Invoke(newValue);
                if (objectIndex < 0 || objectIndex >= GameObjects.Length) return;

                List<Transform> toActivate = GameObjects[objectIndex];
                
                foreach (Transform transform in toActivate)
                    transform?.gameObject.SetActive(true);
            }

            public PTOBinder(List<Transform>[] gameObjects, Func<object, int> indexer)
            {
                GameObjects = gameObjects;
                Indexer = indexer;
            }
        }

        private void PlaceAfterInHierarchy(Transform baseObject, Transform targetObject)
        {
            if (baseObject.parent != targetObject.parent)
            {
                Debug.LogWarning($"Could not reposition {targetObject} to {baseObject}");
                return;
            }

            targetObject.SetSiblingIndex(baseObject.GetSiblingIndex() + 1);
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

        /// <summary>
        /// Looks for special attributes on the serialized member to decorate control, like SetComponentProperty, and
        /// applies them to the created control
        /// </summary>
        private UINode DecorateControl(UINode control)
        {
            if (control == null || control.Type == ControlType.Container) return null;

            List<MemberInfo> members = control.SerializedMembers ?? new List<MemberInfo>() { control.Member };

            foreach (MemberInfo member in members)
            {
                foreach (var attribute in member.GetCustomAttributes<SetComponentPropertyAttribute>())
                {
                    GameObject controlObject = control.Control.gameObject;
                    if (attribute.ChildName != null)
                    {
                        Transform child = control.Control.Find(attribute.ChildName);
                        if (child is null)
                        {
                            Debug.LogError($"Could not find child {attribute.ChildName} on {control.Control} (Member: {control.Member})");
                            continue;
                        }
                        controlObject = child.gameObject;
                    }

                    Component component = controlObject.GetComponentInChildren(attribute.ComponentType, true);
                    HandleNestedProperty(attribute.PropertyName, attribute.Value, component);
                }
            }

            return control;

            static void HandleNestedProperty(string propertyName, object value, object container)
            {
                int dotIndex = propertyName.IndexOf('.');
                Type type = container.GetType();

                if (dotIndex < 0)
                {
                    PropertyInfo property = type.GetProperty(propertyName);
                    property.SetValue(container, value);
                    return;
                }

                string topLevel = propertyName.Substring(0, dotIndex);
                PropertyInfo topLevelProperty = type.GetProperty(topLevel);
                object topLevelValue = topLevelProperty.GetValue(container);

                HandleNestedProperty(propertyName.Substring(dotIndex + 1), value, topLevelValue);

                if (topLevelValue.GetType().IsValueType)
                    topLevelProperty.SetValue(container, topLevelValue);
            }
        }

        private void ApplyLayout(GameObject container, ConfigGroupLayout layout)
        {
            switch (layout)
            {
                case ConfigGroupLayout.Default:
                case ConfigGroupLayout.Vertical:
                    AddVerticalStack(container);
                    break;
                case ConfigGroupLayout.Horizontal:
                    AddHorizontalStack(container);
                    break;
            }
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
            if (!property.CanRead) { Debug.LogWarning("Unreadable properties are not serialized"); return null; }
            var controlCreators = property.CanWrite ? _propertyControlCreators : _readonlyPropertyControlCreators;

            foreach (var creator in controlCreators)
                if (creator.Key(property.PropertyType)) return creator.Value(property, container, parent);

            Debug.LogWarning($"Failed to create control for {property} on {container} : not implemented!");

            return null;
        }

        private UINode CreateColorIndicator(PropertyInfo property, object memberContainer, UINode parent)
        {
            return CreateUniversal<ConfigProperty, ColorIndicator>(
                property, _colorIndicatorPrefab, memberContainer, parent, (x, y, z) =>
                {
                    x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
                },
                nameof(ColorIndicator.Color), ControlType.ColorIndicator, readonlyProperty: true
            );
        }

        private UINode CreateOutputLabel(PropertyInfo property, object memberContainer, UINode parent)
        {
            return CreateUniversal<LabelPropertyAttribute, LabeledUIElement>(
                property, _labelPrefab, memberContainer, parent, null,
                nameof(LabeledUIElement.LabelText), ControlType.OutputLabel, (x, y) =>
                {
                    IObjectConverter<string> converter = y?.GetConverter(MakeControlLabel(property, y)) ?? LabelPropertyAttribute.GetDefaultConverter();

                    // these 3 lines are just making a Func<T, string> out of Func<object, string> (otherwise it doesn't bind to events)
                    var proxy = Activator.CreateInstance(typeof(ObjectConverterProxy<,>).MakeGenericType(property.PropertyType, typeof(string)), converter);
                    MethodInfo typedConverterInfo = proxy.GetType().GetMethod(nameof(ObjectConverterProxy<int, int>.Convert));
                    Delegate typedConverter = typedConverterInfo.CreateDelegate(typeof(Func<,>).MakeGenericType(property.PropertyType, typeof(string)), proxy);

                    return (typedConverter, null);
                }, readonlyProperty: true
            );
        }

        private class ObjectConverterProxy<T, V>
        {
            private IObjectConverter<V> ObjectConverter { get; }

            public V Convert(T value) { return ObjectConverter.Convert(value); }

            public ObjectConverterProxy(IObjectConverter<V> objectConverter) { ObjectConverter = objectConverter; }
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

            return CreateUniversal<DropdownProperty, DropdownList>(
                property, _dropdownPrefab, memberContainer, parent, (x, y, z) =>
                {
                    x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
                    object[] displayedOptions = z?.DisplayedOptions == null ? property.PropertyType.GetEnumNames() : z.DisplayedOptions;
                    string[] names = new string[displayedOptions.Length];
                    for (int i = 0; i < displayedOptions.Length; i++) names[i] = SplitAndLowerCamelCase(displayedOptions[i].ToString());
                    x.SetOptions(new List<string>(z?.OptionNames ?? names));
                },
                nameof(DropdownList.SelectedValue), ControlType.DropdownList, (x, z) =>
                {
                    List<int> mapping = new List<int>();
                    for (int i = 0; i < x.Options.Count; i++) mapping.Add(i);

                    for (int i = 0; i < (z?.DisplayedOptions?.Length ?? 0); i++)
                    {
                        if (z.DisplayedOptions[i].GetType() != property.PropertyType)
                            throw new ArgumentException("Dropdown displayed options contain invalid types");
                        mapping[i] = Convert.ToInt32(z.DisplayedOptions[i]);
                    }

                    return ((Delegate, Delegate))GetType().GetMethod(nameof(GenerateEnumDropdownListConverters), BindingFlags.Static | BindingFlags.NonPublic)
                            .MakeGenericMethod(property.PropertyType).Invoke(null, new object[] { mapping });
                }
            );
        }

        private static (Delegate, Delegate) GenerateEnumDropdownListConverters<T>(List<int> mapping) where T : Enum
        {
            return (
                (Func<T, int>)(x => mapping.IndexOf(Convert.ToInt32(x))),
                (Func<int, T>)(x => (T)Enum.ToObject(typeof(T), mapping[x]))
            );
        }

        /// <summary>
        /// Universal method for creating controls
        /// </summary>
        /// <typeparam name="T">Type of the config property. Use ConfigProperty if no specific type exists</typeparam>
        /// <typeparam name="V">Type of the script for UI control to create</typeparam>
        /// <param name="property">PropertyInfo to bind to the control</param>
        /// <param name="prefab">Prefab for the UI control. The root object should contain component with type `V`</param>
        /// <param name="memberContainer">The object that defines target property (and corresponding PropertyChanged event)</param>
        /// <param name="parent">UINode that should become a parent for the new control</param>
        /// <param name="initDelegate">Delegate for control initialization. Should accept three arguments: instance of UI control (type `V`), 
        ///		instance of ConfigProperty, and instance of attribute of type `V`. The third argument may be null if no such attribute was found</param>
        /// <param name="propertyName">Property name on the type `V` that should be bound to target property</param>
        /// <param name="controlType">The type of control being created</param>
        /// <param name="converterGenerator">Function that returns two delegates: first converts property value to UI, second from UI to property value.
        ///		If the target property and property on UI control have the same type and do not require conversion, leave this argument as null.
        ///		Simple converter should accept value from one property, and return a value to assign to another property</param>
        /// <param name="readonlyProperty">When `true`, UI control can only display the value of the property, but cannot change it</param>
        /// <returns></returns>
        private UINode CreateUniversal<T, V>(PropertyInfo property, GameObject prefab, object memberContainer,
            UINode parent, Action<V, ConfigProperty, T> initDelegate, string propertyName, ControlType controlType,
            Func<V, T, (Delegate prop2ui, Delegate ui2prop)> converterGenerator = null, bool readonlyProperty = false) where T : ConfigProperty where V : Component
        {
            ConfigProperty configProperty = GetConfigProperty<ConfigProperty>(property, memberContainer);
            T specificAttribute = configProperty as T;
            Type configType = configProperty.GetType();

            if (specificAttribute == null && configType != typeof(ConfigProperty))
            {
                Debug.LogError($"Serialization error: attribute of type {configType} encountered on the property of type {property.PropertyType}");
                return null;
            }

            GameObject newControl = Instantiate(prefab, parent.Control);
            V specificControl = newControl.GetComponent<V>();
            if (specificControl is null)
            {
                Debug.LogError($"Prefab {prefab} does not contain a component of type {typeof(V)}");
                return null;
            }
            if (initDelegate is { }) initDelegate(specificControl, configProperty, specificAttribute);
            var specificProperty = typeof(V).GetProperty(propertyName);
            var controlEvent = GetEvent(specificProperty);
            Delegate handler = (Delegate)(
                readonlyProperty
                ?
                    (converterGenerator is null ? MakeDirectionalHandler(specificProperty, specificControl, property.PropertyType) : null)
                : 
                GetType().GetMethod(nameof(GetUniversalHandler), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(property.PropertyType).Invoke(null, new object[] { property, specificProperty, memberContainer, specificControl })
            );
            Delegate propToUiHandler = handler, uiToPropHandler = handler;
            if (converterGenerator != null)
            {
                (Delegate prop2ui, Delegate ui2prop) = converterGenerator(specificControl, specificAttribute);
                propToUiHandler = MakeDirectionalHandler(specificProperty, specificControl, property.PropertyType, prop2ui);
                if (!readonlyProperty) uiToPropHandler = MakeDirectionalHandler(property, memberContainer, specificProperty.PropertyType, ui2prop);

                specificProperty.SetValue(specificControl, prop2ui.DynamicInvoke(property.GetValue(memberContainer)));
            } else
            {
                specificProperty.SetValue(specificControl, property.GetValue(memberContainer));
            }

            // TODO: I don't think we get rid of poll handlers when the UI control is destroyed :( need to fix
            if (!readonlyProperty) controlEvent.AddEventHandler(specificControl, uiToPropHandler);
            if (configProperty.HasEvent)
            {
                var parentEvent = GetEvent(property);
                if (parentEvent == null)
                {
                    if (configProperty.IsPollingAllowed == null) 
                        Debug.LogWarning($"Event not found for property {property} on {memberContainer}; Falling back to polling");

                    if (configProperty.IsPollingAllowed.GetValueOrDefault(true))
                        OnPoll += () => propToUiHandler.DynamicInvoke(property.GetValue(memberContainer));
                }
                else
                    parentEvent.AddEventHandler(memberContainer, propToUiHandler);
            } else if (configProperty.IsPollingAllowed.GetValueOrDefault(false)) {
                OnPoll += () => propToUiHandler.DynamicInvoke(property.GetValue(memberContainer));
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
                // first check which doesn't have the new value, then set; otherwise bad recursion stuff may happen
                bool set1 = !Equals(prop1.GetValue(cont1), x);
                bool set2 = !Equals(prop2.GetValue(cont2), x);
                if (set1) prop1.SetValue(cont1, x);
                if (set2) prop2.SetValue(cont2, x);
            });
        }

        private static Delegate MakeDirectionalHandler(PropertyInfo targetProperty, object declaringObject, Type eventValueType = null, Delegate converter = null)
        {
            if (eventValueType is null) eventValueType = targetProperty.PropertyType;

            return (Delegate)typeof(ConfigMenuSerializer).GetMethod(nameof(GetDirectionalHandler), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(eventValueType, targetProperty.PropertyType).Invoke(null, new object[] { targetProperty, declaringObject, converter });
        }

        /// <summary>
        /// property is a Property that is going to be updated by this handler
        /// property has type S, whereas input is type T
        /// </summary>
        private static Delegate GetDirectionalHandler<T, S>(PropertyInfo property, object container, Delegate converter)
        {
            if (converter == null)
            {
                if (typeof(T) == typeof(S)) return (Action<T>)(x => property.SetValue(container, x));
                throw new ArgumentNullException(nameof(converter), "Cannot automatically create a converter");
            }

            Func<T, S> t2o = (Func<T, S>)converter;
            return (Action<T>)(x => property.SetValue(container, t2o(x)));
        }

        private UINode CreateToggle(PropertyInfo property, object memberContainer, UINode parent)
        {
            if (GetConfigProperty<RadioButtonsProperty>(property, memberContainer) is { })
                return CreateRadioButtons(property, memberContainer, parent);

            return CreateUniversal<ConfigProperty, Toggle>(
                property, _togglePrefab, memberContainer, parent, (x, y, z) => { x.LabelText = MakeControlLabel(property, y); },
                nameof(Toggle.IsChecked), ControlType.Toggle
            );
        }

        /// <summary>
        /// Extended version of property.GetCustomAttribute. This method should be used in order to support proxied property
        /// serialization (e.g. for nullable properties)
        /// </summary>
        /// <typeparam name="T">Attribute type to get</typeparam>
        /// <param name="property">The property</param>
        /// <param name="declaringObject">The object defining the property</param>
        /// <returns>Instance of the requested attribute, or null if it is not found</returns>
        private T GetConfigProperty<T>(PropertyInfo property, object declaringObject) where T : ConfigProperty
        {
            if (typeof(IPropertyProxy).IsAssignableFrom(property.DeclaringType))
                return declaringObject.GetType().GetProperty(nameof(IPropertyProxy.Attribute)).GetValue(declaringObject) as T;

            return property.GetCustomAttribute<T>();
        }

        private UINode CreateUnboundedListView(PropertyInfo property, object memberContainer, UINode parent)
        {
            if (GetConfigProperty<ListViewProperty>(property, memberContainer) is null) {
                Debug.LogWarning($"Cannot serialize a List property {property} without ViewViewProperty attribute");
                return null;
            }

            var elementType = property.PropertyType.GetGenericArguments()[0];

            return CreateUniversal<ListViewProperty, UnboundedListView>(
                property, _unboundedListViewPrefab, memberContainer, parent, (x, y, z) =>
                {
                    // Figure out item options
                    var itemOptions = new List<UnboundedListView.ListItemType>();
                    if (z.SourcePropertyName is null) {
                        int length = z.OptionNames?.Length ?? z.DisplayedOptions?.Length ?? 0;
                        if (length == 0) Debug.LogWarning($"Generating a list for {property} on {memberContainer} with 0 option types");
                        for (int i = 0; i < length; i++)
                            itemOptions.Add(new UnboundedListView.ListItemType() { 
                                Name = z.OptionNames?[i] ?? z.DisplayedOptions[i].ToString(), 
                                Data = z.DisplayedOptions?[i] ?? z.OptionNames[i]
                            });
                    } else {
                        var options = memberContainer.GetType().GetProperty(z.SourcePropertyName)?.GetValue(memberContainer) as Dictionary<string, object>;
                        if (options is null) {
                            Debug.LogWarning("SourceProperty of {property} on {memberContainer} is not of type Dictionary<string, object> or the property is missing");
                            options = new();
                        }

                        foreach (var entry in options)
                            itemOptions.Add(new UnboundedListView.ListItemType() { Name = entry.Key, Data = entry.Value });
                    }

                    // Actual init
                    x.LabelText = MakeControlLabel(property, y);
                    x.ElementType = elementType;
                    x.SetItemOptions(itemOptions);
                },
                nameof(UnboundedListView.Items), ControlType.UnboundedListView, (x, y) =>
                {
                    var caster = typeof(ConfigMenuSerializer).GetMethod(nameof(GetListCaster), BindingFlags.Static | BindingFlags.NonPublic);

                    var prop2uiConverter = (Delegate)caster.MakeGenericMethod(elementType, typeof(object)).Invoke(null, Array.Empty<object>());
                    var ui2propConverter = (Delegate)caster.MakeGenericMethod(typeof(object), elementType).Invoke(null, Array.Empty<object>());

                    return (prop2uiConverter, ui2propConverter);
                }
            );
        }

        /// <summary>
        /// Makes a delegate that casts input List of Fs to a List of Ts
        /// </summary>
        private static Delegate GetListCaster<F, T>() => (Func<List<F>, List<T>>)(x => x.Cast<T>().ToList());

        private UINode CreateNullableControl(PropertyInfo property, object memberContainer, UINode parent)
        {
            Type nullableType = property.PropertyType.GetGenericArguments()[0];
            Type proxyType = typeof(NullableBinderProxy<>).MakeGenericType(nullableType);
            ConfigProperty configProperty = GetConfigProperty<ConfigProperty>(property, memberContainer);
            object proxy = Activator.CreateInstance(proxyType, configProperty);
            PropertyInfo hasValueProperty = proxyType.GetProperty(nameof(NullableBinderProxy<int>.HasValue));
            PropertyInfo valueProperty = proxyType.GetProperty(nameof(NullableBinderProxy<int>.Value));
            PropertyInfo nullableValueProperty = proxyType.GetProperty(nameof(NullableBinderProxy<int>.NullableValue));
            UINode container = CreateContainer(parent, $"Nullable control for {property.Name}");

            GetEvent(property)?.AddEventHandler(memberContainer, MakeDirectionalHandler(nullableValueProperty, proxy));
            GetEvent(nullableValueProperty).AddEventHandler(proxy, MakeDirectionalHandler(property, memberContainer));

            PropertyInfo name = typeof(ConfigProperty).GetProperty(nameof(ConfigProperty.Name));

            string configPropertyName = name.GetValue(configProperty) as string;
            bool nullName = configPropertyName is null;
            if (nullName) name.SetValue(configProperty, MakeControlLabel(property, configProperty)); 
            UINode toggle = CreateToggle(hasValueProperty, proxy, container);
            UINode valueContainer = CreateContainer(container);
            IndentContainer(valueContainer, 20);
            if (nullName) name.SetValue(configProperty, null);

            name.SetValue(configProperty, "Value");
            UINode valueControl = CreatePropertyControl(valueProperty, proxy, valueContainer);
            name.SetValue(configProperty, configPropertyName);

            ApplyLayout(valueContainer.Control.gameObject, ConfigGroupLayout.Default);
            ApplyLayout(container.Control.gameObject, ConfigGroupLayout.Default);
            BindToggleToObject(toggle.Control.gameObject, valueContainer.Control);

            nullableValueProperty.SetValue(proxy, property.GetValue(memberContainer));

            return new UINode(parent)
            {
                Control = container.Control,
                Member = property,
                MemberContainer = memberContainer,
                Type = ControlType.NullableControl,
                Metadata = null
            };
        }

        private interface IPropertyProxy { public ConfigProperty Attribute { get; } }

        private class NullableBinderProxy<T> : IPropertyProxy where T : struct
        {
            private T _value;
            private bool _hasValue;
            private readonly ConfigProperty _attribute;

            public T Value
            {
                get => _value;
                set { if (Equals(_value, value)) return; _value = value; HasValue = true; ValueChanged?.Invoke(_value); EmitChange(); }
            }

            public bool HasValue
            {
                get => _hasValue;
                set { if (_hasValue == value) return; _hasValue = value; HasValueChanged?.Invoke(_hasValue); EmitChange(); }
            }

            public T? NullableValue
            {
                get => HasValue ? Value : new Nullable<T>();
                set
                {
                    if (!HasValue && !value.HasValue) return;
                    if (HasValue && value.HasValue && Equals(Value, value.Value)) return;
                    HasValue = value.HasValue;
                    if (HasValue) Value = value.Value;
                    NullableValueChanged?.Invoke(NullableValue);
                }
            }

            private void EmitChange() => NullableValueChanged?.Invoke(NullableValue);

            public ConfigProperty Attribute => _attribute;

            public event Action<T> ValueChanged;
            public event Action<bool> HasValueChanged;
            public event Action<T?> NullableValueChanged;

            public NullableBinderProxy(ConfigProperty attribute) { _attribute = attribute; }
        }

        private UINode CreateStringPropertyControl(PropertyInfo property, object memberContainer, UINode parent)
        {
            if (GetConfigProperty<FilePathProperty>(property, memberContainer) is { })
                return CreateFilePathSelector(property, memberContainer, parent);

            if (GetConfigProperty<DropdownProperty>(property, memberContainer) is { })
                return CreateTextDropdownList(property, memberContainer, parent);

            return CreateTextInputField(property, memberContainer, parent);
        }

        private UINode CreateTextDropdownList(PropertyInfo property, object memberContainer, UINode parent)
        {
            return CreateUniversal<DropdownProperty, DropdownList>(
                property, _dropdownPrefab, memberContainer, parent, (x, y, z) =>
                {
                    x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
                    if (z.DisplayedOptions is null) Debug.LogWarning($"DisplayedOptions property should be specified for string dropdown ({property} on {memberContainer})");
                    object[] displayedOptions = z.DisplayedOptions ?? new object[0];
                    string[] names = new string[displayedOptions.Length];
                    for (int i = 0; i < displayedOptions.Length; i++) names[i] = SplitAndLowerCamelCase(displayedOptions[i].ToString());
                    x.SetOptions(new List<string>(z.OptionNames ?? names));
                },
                nameof(DropdownList.SelectedValue), ControlType.DropdownList, (x, z) =>
                {
                    List<string> mapping = new List<string>();
                    for (int i = 0; i < x.Options.Count; i++) mapping.Add(z.DisplayedOptions[i].ToString());

                    return (
                        (Func<string, int>)(x => mapping.IndexOf(x)),
                        (Func<int, string>)(x => mapping[x])
                    );
                }
            );
        }

        private UINode CreateTextInputField(PropertyInfo property, object memberContainer, UINode parent)
        {
            return CreateUniversal<ConfigProperty, InputField>(
                property, _textInputFieldPrefab, memberContainer, parent, (x, y, z) =>
                {
                    x.LabelText = z.Name ?? SplitAndLowerCamelCase(property.Name);
                },
                nameof(InputField.Text), ControlType.TextInputField
            );
        }

        private UINode CreateFilePathSelector(PropertyInfo property, object memberContainer, UINode parent)
        {
            FilePathProperty filePathProperty = GetConfigProperty<FilePathProperty>(property, memberContainer);

            return CreateUniversal<FilePathProperty, FilePathSelector>(
                property, _filePathSelectorPrefab, memberContainer, parent, (x, y, z) =>
                {
                    x.LabelText = z.Name ?? SplitAndLowerCamelCase(property.Name);
                    x.FileNameDisplayedConverter = z.StringConverter;
                    x.CheckFileExists = z.CheckFileExists;
                    x.FileFilters = z.Filters;
                    x.DialogTitle = z.DialogTitle;
                    x.FileDialogPlugin = z.EnablePlugin;
                },
                nameof(FilePathSelector.SelectedPath), ControlType.FilePathSelector
            );
        }

        private string MakeControlLabel(PropertyInfo property, ConfigProperty configProperty) => configProperty.Name ?? SplitAndLowerCamelCase(property.Name);

        private UINode CreateNumericPropertyControl(PropertyInfo property, object memberContainer, UINode parent)
        {
            if (GetConfigProperty<MinMaxSliderProperty>(property, memberContainer) is { })
                return CreateMinMaxSlider(property, memberContainer, parent);

            if (GetConfigProperty<InputFieldPropertyAttribute>(property, memberContainer) is { })
                return CreateNumericInputField(property, memberContainer, parent);

            return CreateSlider(property, memberContainer, parent);
        }

        private UINode CreateNumericInputField(PropertyInfo property, object memberContainer, UINode parent)
        {
            bool isInt = IsIntegral(property.PropertyType);

            return CreateUniversal<InputFieldPropertyAttribute, NumericInputField>(
                property, _numericInputFieldPrefab, memberContainer, parent, (x, y, z) =>
                {
                    int intValue = isInt ? (int)property.GetValue(memberContainer) : 0;
                    float floatValue = isInt ? intValue : (float)property.GetValue(memberContainer);

                    x.MinValue = z?.MinValue ?? float.MinValue;
                    x.MaxValue = z?.MaxValue ?? float.MaxValue;
                    x.InputFormatting = z?.InputFormatting ?? (isInt ? "0" : "0.000");
                    x.InputRegex = z?.InputRegex ?? @"([-+]?[0-9]*\.?[0-9]+)";
                    x.RegexGroupIndex = z?.RegexGroupIndex ?? 1;
                    x.WholeNumbers = isInt;
                    x.LabelText = y.Name ?? SplitAndLowerCamelCase(property.Name);
                },
                isInt ? nameof(NumericInputField.IntValue) : nameof(NumericInputField.Value), ControlType.NumericInputField
            );
        }

        private UINode CreateSlider(PropertyInfo property, object memberContainer, UINode parent)
        {
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
            RadioButtonsProperty radioButtonsProperty = GetConfigProperty<RadioButtonsProperty>(property, memberContainer);
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
            MinMaxSliderProperty sliderProperty = GetConfigProperty<MinMaxSliderProperty>(property, memberContainer);
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

        private UINode CreateContainer(UINode parent, string inspectorName = null)
        {
            GameObject container = new GameObject(inspectorName ?? "Container"); // Default game object name is longer...
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

        private HorizontalUIStack AddHorizontalStack(GameObject gameObject)
        {
            HorizontalUIStack horizontalStack = gameObject.AddComponent<HorizontalUIStack>();
            horizontalStack.ControlContainerHeight = true;
            horizontalStack.LeftMargin = 2;
            horizontalStack.Spacing = 10;
            return horizontalStack;
        }

        private static EventInfo GetEvent(PropertyInfo property) => property.DeclaringType.GetEvent(property.Name + "Changed");

        private void Awake() { MainInstance ??= this; }
        private void Start() { if (_serializeOnStart) { GenerateMenuUI(); _tabView.SelectTab(_selectedTab); } }

        private float _lastPoll;
        private float PollInterval => 1 / PollFrequency;

        private event Action OnPoll;

        public float PollFrequency { get => _pollFrequency; set => _pollFrequency = value; }

        private void Update()
        {
            float timeSinceLast = Time.unscaledTime - _lastPoll;
            if (timeSinceLast > PollInterval)
            {
                OnPoll?.Invoke();
                _lastPoll = Time.unscaledTime;
            }
        }

        public ConfigMenuSerializer() : base()
        {
            _propertyControlCreators = new Dictionary<Func<Type, bool>, Func<PropertyInfo, object, UINode, UINode>>()
            {
                { x => x.IsEnum, CreateDropdownList },
                { x => IsIntegral(x) || x == typeof(float) || x == typeof(double), CreateNumericPropertyControl },
                { x => x == typeof(bool), CreateToggle },
                { x => x == typeof(Color), CreateColorButton },
                { x => x == typeof(Gradient), CreateGradientButton },
                { x => x == typeof(AnimationCurve), CreateCurveButton },
                { x => x == typeof(Texture2D), CreateTextureButton },
                { x => x == typeof(string), CreateStringPropertyControl },
                { x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>), CreateNullableControl },
                { x => typeof(System.Collections.IList).IsAssignableFrom(x), CreateUnboundedListView }
            };
            _readonlyPropertyControlCreators = new Dictionary<Func<Type, bool>, Func<PropertyInfo, object, UINode, UINode>>()
            {
                { x => x == typeof(Color), CreateColorIndicator },
                { x => true, CreateOutputLabel }
            };
        }
    }
}
