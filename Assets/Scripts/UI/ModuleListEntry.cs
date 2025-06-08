using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityCore;
using Core;
using ConfigSerialization;
using UnityEngine.EventSystems;

namespace ConstellationUI
{
    public class ModuleListEntry : ListEntryBase
    {
        [Header("Objects")]
        [SerializeField] private DragHandle _dragHandle;
        [SerializeField] private Toggle _enableToggle;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private RectTransform _container;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private CanvasGroup _containerCanvasGroup;
        [SerializeField] private GameObject _dragItemPrefab;
        [SerializeField] private UnityEngine.UI.Image _mainGraphic;
        [SerializeField] private GameObject _quickTogglePrefab;

        [Header("Decorative parameters")]
        [SerializeField] private List<GameObject> _extendedElements;
        [SerializeField] private float _extendedFontSize = 24;
        [SerializeField] private float _compactFontSize = 36;
        [SerializeField] private float _extendedToggleHeight = 32;
        [SerializeField] private float _compactToggleHeight = 50;
        [SerializeField] private Color _extendedColor = Color.grey;
        [SerializeField] private Color _compactColor = Color.white;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _draggingSprite;
        
        [Header("State")]
        [SerializeField] private bool _extended;

        private RectTransform _enableToggleTransform;
        private int _containedChildrenCount;
        private MonoEvents _monoEvents;
        private MonoDraggable _draggable;

        public override TextMeshProUGUI WrappedLabel => _enableToggle.WrappedLabel;

        public override object EntryData { get => Data; set => throw new NotSupportedException(); }
        public override int EntryIndex { get => Index; set => throw new NotSupportedException(); }
        public override RectTransform Container => _container;
        public override bool Locked { get => ModuleDescriptor.Locked; set => throw new NotSupportedException(); }
        public ModuleDescriptor ModuleDescriptor => EntryData as ModuleDescriptor;

        private RectTransform EnableToggleTransform => _enableToggleTransform ?? (_enableToggleTransform = _enableToggle.GetComponent<RectTransform>());
        private MonoEvents MonoEvents => _monoEvents ?? (_monoEvents = GetComponent<MonoEvents>());

        public void SetExtended(bool extended) {
            _extended = extended;

            foreach (GameObject gameObject in _extendedElements)
                gameObject.SetActive(extended);

            WrappedLabel.fontSize = extended ? _extendedFontSize : _compactFontSize;
            WrappedLabel.color = extended ? _extendedColor : _compactColor;
            _enableToggle.WrappedToggle.graphic.color = extended ? _extendedColor : _compactColor;
            EnableToggleTransform.sizeDelta = new Vector2(EnableToggleTransform.sizeDelta.x, extended ? _extendedToggleHeight : _compactToggleHeight);
        }

        public override void Initialize(object data, int index, Type dataType = null) {
            Index = index;
            Data = data;

            // data is either a ModuleDescriptor, or an object from which a descriptor can be constructed
            if (!dataType.IsAssignableFrom(data.GetType())) {
                Data = Activator.CreateInstance(dataType, data);
            }

            LabelText = ModuleDescriptor.Name;
            _enableToggle.IsChecked = ModuleDescriptor.Enabled;
            _canvasGroup.alpha = ModuleDescriptor.Locked ? 0.5f : 1;
            _canvasGroup.interactable = !ModuleDescriptor.Locked;
            SetExtended(_extended);
            OnChildrenUpdated();

            TextureManager textures = FindFirstObjectByType<TextureManager>(FindObjectsInactive.Include);
            var quickToggles = ModuleDescriptor.GetQuickToggles();
            ModuleDescriptor.QuickToggleStates ??= new List<int>();
            for (int i = 0; i < quickToggles.Count; i++) {
                var quickToggle = quickToggles[i];
                Toggle toggle = Instantiate(_quickTogglePrefab, _quickTogglePrefab.transform.parent).GetComponent<Toggle>();
                toggle.gameObject.SetActive(true);
                toggle.Icon = TextureManager.ToSprite(textures.FindTexture(quickToggle.icon)?.Texture);
                if (ModuleDescriptor.QuickToggleStates.Count >= i + 1) {
                    toggle.WrappedToggle.isOn = ModuleDescriptor.QuickToggleStates[i] > 0;
                    toggle.IsChecked = toggle.WrappedToggle.isOn;
                } else 
                    ModuleDescriptor.QuickToggleStates.Add(toggle.IsChecked ? 1 : 0);

                int copy = i;
                toggle.IsCheckedChanged += x => {
                    ModuleDescriptor.QuickToggleStates[copy] = x ? 1 : 0;
                    EmitItemChanged();
                };
            }

            if (!ModuleDescriptor.HasProperties) return;
            MonoEvents events = Container.gameObject.GetOrAddComponent<MonoEvents>();
            events.OnRectTransformChange += OnChildrenUpdated;

            ConfigMenuSerializer.MainInstance.GenerateMenuUI(Container, ModuleDescriptor.ModuleData, 0);
        }

        private void OnEnabledCheckedChanged(bool value) {
            ModuleDescriptor.Enabled = value;
            _containerCanvasGroup.alpha = value ? 1 : 0.5f;
            EmitItemChanged();
        }

        private void OnDragHandleClick(PointerEventData eventData) {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            SetExtended(!_extended);
        }

        private void OnDrag(Vector3 position)
        {
            if (ModuleDescriptor.Locked) return;
            
            EmitDrag(position);
        }

        public override void OnIndexChanged(int index) { Index = index; }

        private void OnChildrenUpdated() {
            if (_containedChildrenCount == Container.childCount) return;

            _containedChildrenCount = Container.childCount;
            SetExtended(true);
        }

        protected void Awake() {
            _deleteButton.Click += EmitItemRemoved;
            _enableToggle.IsCheckedChanged += OnEnabledCheckedChanged;
            _dragHandle.DragStart += OnDragStart;
            _dragHandle.DragEnd += OnHandleDragEnd;
            _dragHandle.Click += OnDragHandleClick;
        }
        
        // Even though we create a new object when drag is started, it is our handle that emits DragEnd (since it captured OnPointerDown)
        private void OnHandleDragEnd() {
            if (ModuleDescriptor.Locked) return;

            _draggable.FinishDrag();
            _canvasGroup.alpha = 1;
            _mainGraphic.sprite = _normalSprite;

            Vector3 finalPosition = _draggable.Position;
            Destroy(_draggable.gameObject);
            _draggable = null;
            EmitDragDrop(finalPosition);
        }

        private void OnDragStart()
        {
            if (ModuleDescriptor.Locked) return;
            _canvasGroup.alpha = 0.8f;
            _mainGraphic.sprite = _draggingSprite;

            RectTransform canvas = transform.GetComponent<RectTransform>(), tmp;
            while ((tmp = canvas.parent.GetComponent<RectTransform>()) != null)
                canvas = tmp;

            _draggable = Instantiate(_dragItemPrefab, canvas).GetComponent<MonoDraggable>();
            _draggable.BeginDrag(Vector2.zero);
            _draggable.GetComponent<LabeledUIElement>().LabelText = ModuleDescriptor.Name;
            _draggable.PositionChanged += OnDrag;
            EmitDragStart();

            // presumably due to ChildWrapper + Text change the drag item's position visibly changes upon creation
            // so we hide it for a few frames to wait for ChildWrapper and MonoDraggable to stabilize
            var canvasGroup = _draggable.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            Destroy(canvasGroup, 0.02f);
        }
    }
}
