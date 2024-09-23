using System.Collections.Generic;
using System.Collections;
using UnityCore;
using UnityEngine;
using UnityEngine.UI;

namespace ConstellationUI
{
    public class TabView : MonoBehaviour
    {
        [Header("Objects")]
        [SerializeField] private GameObject _tabButtonPrefab;
        [SerializeField] private RectTransform _tabButtonsContainer;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private List<Tab> _tabs = new List<Tab>();

        [Header("Parameters")]
        [SerializeField] private bool _resetScrollRectPosition;
        [SerializeField] private bool _resizeScrollRectContent;
        [SerializeField] private int _selectedIndex;

        private ToggleGroup _tabButtonsGroup;
        private List<Toggle> _tabButtons = new List<Toggle>();

        [System.Serializable] public struct Tab
        {
            public string Name;
            public RectTransform Contents;
        }

        public void SelectTab(int index)
        {
            if (index == _selectedIndex) return;
            SetTabActive(_selectedIndex, false);
            _selectedIndex = index;
            SetTabActive(_selectedIndex, true);

            if (_resetScrollRectPosition)
            {
                _scrollRect.verticalNormalizedPosition = 1;
                _scrollRect.horizontalNormalizedPosition = 0;
            }

            if (_resizeScrollRectContent) ResizeScrollRectContent();
        }

        public void ClearAllTabs()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                Destroy(_tabs[i].Contents.gameObject);
                Destroy(_tabButtons[i].gameObject);
            }

            _tabs.Clear();
            _tabButtons.Clear();
            _selectedIndex = -1;
        }

        public RectTransform AddTab(string name)
        {
            GameObject newTab = new GameObject(name);
            RectTransform tabTransform = newTab.AddComponent<RectTransform>();
            tabTransform.SetParent(_scrollRect.content, false);
            tabTransform.pivot = new Vector2(0.5f, 1);
            tabTransform.anchorMin = new Vector2(0, 1);
            tabTransform.anchorMax = new Vector2(1, 1);
            tabTransform.offsetMin = new Vector2(0, tabTransform.offsetMin.y);
            tabTransform.offsetMax = new Vector2(0, tabTransform.offsetMax.y);

            RegisterTab(tabTransform, name);

            return tabTransform;
        }

        private void RegisterTab(RectTransform content, string name)
        {
            Tab newTab = new Tab() { Contents = content, Name = name };
            _tabs.Add(newTab);
            RegisterTab(newTab);
        }

        private void RegisterTab(Tab tab)
        {
            MonoEvents events = tab.Contents.gameObject.GetOrAddComponent<MonoEvents>();
            events.OnRectTransformChange += OnTabRectTransformChange;
            tab.Contents.gameObject.SetActive(false);

            CreateTabButton(tab.Name);
        }

        private void CreateTabButton(string name)
        {
            GameObject tabButton = Instantiate(_tabButtonPrefab, _tabButtonsContainer);
            Toggle toggle = tabButton.GetComponent<Toggle>();
            toggle.IsCheckedChanged += OnSomeToggleChanged;
            toggle.LabelText = name;
            toggle.ToggleGroup = _tabButtonsGroup;
            _tabButtons.Add(toggle);

            StartCoroutine(ForceLayoutUpdate(_tabButtonsContainer));
        }

        private IEnumerator ForceLayoutUpdate(RectTransform root)
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        private void ResizeScrollRectContent()
        {
            if (_selectedIndex < 0) return;

            RectTransform content = _scrollRect.content;
            Rect targetRect = _tabs[_selectedIndex].Contents.rect;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetRect.width);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetRect.height);
        }

        private void SetTabActive(int index, bool isActive)
        {
            if (index < 0) return;

            _tabs[index].Contents.gameObject.SetActive(isActive);
            _tabButtons[index].IsChecked = isActive;
        }

        private void OnTabRectTransformChange()
        {
            if (_resizeScrollRectContent) ResizeScrollRectContent();
        }

        private void OnSomeToggleChanged(bool obj)
        {
            int index = _tabButtons.FindIndex(x => x.WrappedToggle == _tabButtonsGroup.GetFirstActiveToggle());
            if (index < 0) return;
            SelectTab(index);
        }

        private void Awake()
        {
            _tabButtonsGroup = _tabButtonsContainer.gameObject.AddComponent<ToggleGroup>();
            _tabButtonsGroup.allowSwitchOff = false;

            foreach (Tab tab in _tabs) RegisterTab(tab);

            SetTabActive(_selectedIndex, true);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                MonoEvents events = _tabs[i].Contents.gameObject.GetComponent<MonoEvents>();
                if (events == null) return;
                Destroy(events);
            }
        }
    }
}