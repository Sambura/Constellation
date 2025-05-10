using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using System.Linq;

namespace ConstellationUI
{
    public class UnboundedListView : LabeledUIElement
    {
        [Header("Objects")]
        [SerializeField] private UnityEngine.UI.Toggle _collapseToggle;
        [SerializeField] private TextMeshProUGUI _itemCountLabel;
        [SerializeField] private Button _addItemButton;
        [SerializeField] private DropdownList _itemTypesDropdown;
        [SerializeField] private RectTransform _itemContainer;
        [SerializeField] private RectTransform _itemReorderIndicator;
        [SerializeField] private Color _legalReorderColor;
        [SerializeField] private Color _illegalReorderColor;

        [Header("Parameters")]
        [SerializeField] private GameObject _itemPrefab;

        private List<ListItemType> _itemTypes = new List<ListItemType>();
        private List<ListEntryBase> _listEntries;
        private UnityEngine.UI.ScrollRect _parentScrollRect;
        private Coroutine _scroller;

        private UnityEngine.UI.ScrollRect ParentScrollRect {
            get {
                if (_parentScrollRect is { }) return _parentScrollRect;

                Transform t = transform;
                while ((_parentScrollRect = t.gameObject.GetComponent<UnityEngine.UI.ScrollRect>()) == null)
                    t = t.parent;

                return _parentScrollRect;
            }
        }
        private RectTransform ParentRectTransform => ParentScrollRect.GetComponent<RectTransform>();

        public Type ElementType { get; set; }
        public List<object> Items { 
            get => _listEntries?.Select(x => x.EntryData)?.ToList() ?? new List<object>(); // makes a copy (i think?)
            set {
                if (value is null) {
                    DataToListEntries(null);
                    _listEntries = null;
                    ItemsChanged?.Invoke(Items);
                    return;
                }

                var copy = new List<object>(value);
                if (_listEntries is { } && _listEntries.Select(x => x.EntryData).SequenceEqual(copy))
                    return;

                DataToListEntries(copy);
                ItemsChanged?.Invoke(Items);
            }
        }

        public event Action<List<object>> ItemsChanged;

        public struct ListItemType {
            public string Name;
            public object Data;
        }

        public void SetItemOptions(List<ListItemType> options) {
            _itemTypes = new List<ListItemType>(options);
            List<string> optionNames = _itemTypes.Select(x => x.Name).ToList();

            _itemTypesDropdown.SelectedValueChanged -= AddItemByIndex;
            _itemTypesDropdown.SetOptions(optionNames);
            _itemTypesDropdown.SelectedValue = 0;
            _itemTypesDropdown.SelectedValueChanged += AddItemByIndex;
        }

        private void DataToListEntries(List<object> list)
        {
            if (_listEntries is not null) {
                foreach (var item in _listEntries)
                    Destroy(item.gameObject);
            }

            if (list is null || list.Count == 0) return;

            _listEntries = new List<ListEntryBase>();
            foreach (var item in list) AddItem(item);
        }

        private void OnCollapseToggleClicked(bool value) {
            _itemContainer.gameObject.SetActive(value);
        }

        private void OnItemsChanged(List<object> obj) {
            bool empty = obj.Count == 0;
            _collapseToggle.interactable = !empty;
            if (empty) _collapseToggle.isOn = false;
            _itemCountLabel.text = obj is null ? "0" : obj.Count.ToString();
        }

        private void AddItemByIndex(int typeIndex) {
            if (typeIndex < 0) return;

            var itemInfo = _itemTypes[typeIndex];

            AddItem(itemInfo.Data);
            ItemsChanged?.Invoke(Items);
            _collapseToggle.isOn = true;
        }

        private void AddItem(object data)
        {
            _listEntries ??= new List<ListEntryBase>();
            ListEntryBase newItem = Instantiate(_itemPrefab, _itemContainer).GetComponent<ListEntryBase>();
            newItem.Initialize(data, _listEntries.Count, ElementType);
            _listEntries.Add(newItem);

            newItem.RemoveItem += () => OnItemRemove(newItem);
            newItem.Drag += x => OnItemDrag(newItem, x);
            newItem.DragDrop += x => OnItemDragDrop(newItem, x);
            newItem.ItemChanged += () => ItemsChanged?.Invoke(Items);
        }

        private System.Collections.IEnumerator StartScrolling(float velocity, float maxScroll) {
            float totalScroll = 0;
            while (true) {
                float scrollAmount = velocity * Time.deltaTime;
                ParentScrollRect.content.position += new Vector3(0, scrollAmount);
                totalScroll += scrollAmount;
                if (Mathf.Abs(totalScroll) > maxScroll) break;
                yield return null;
            }
            _scroller = null;
        }

        // -1 if no reorder can/should be performed
        private int ResolveReorder(ListEntryBase targetEntry, Vector2 targetPosition, bool indicateTargetPosition)
        {
            // if anything, this ensures that scrolling is done in the correct direction :)
            if (_scroller is { }) StopCoroutine(_scroller);
            _scroller = null;

            // if too much horizontal deviation - cancel reorder
            Vector3[] corners = new Vector3[4];
            RectTransform.GetWorldCorners(corners);
            float left = corners[0].x, right = corners[2].x, width = right - left;
            float tx = targetPosition.x;
            bool outOfBounds = (tx < left && (left - tx > width / 2)) || (tx > right && (tx - right > width / 2));
            if (outOfBounds) {
                _itemReorderIndicator.gameObject.SetActive(false);
                return -1;
            }

            // check if we need to scroll parent ScrollRect to reveal more of our items
            float bottom = corners[0].y, top = corners[1].y;
            ParentRectTransform.GetWorldCorners(corners);
            float parentBottom = corners[0].y, parentTop = corners[1].y;
            float scrollThreshold = 35;
            float overscroll = 15;
            float scrollSpeed = parentTop - parentBottom;
            bool wantScrollUp = targetPosition.y + scrollThreshold > parentTop;
            bool wantScrollDown = targetPosition.y - scrollThreshold < parentBottom;
            float scrollVelocity = 0, maxScroll = 0;

            if (wantScrollUp && top > parentTop - overscroll) {
                scrollVelocity = -scrollSpeed;
                maxScroll = top - parentTop + overscroll;
            } else if (wantScrollDown && bottom < parentBottom + overscroll) {
                scrollVelocity = scrollSpeed;
                maxScroll = parentBottom - bottom + overscroll;
            }

            if (scrollVelocity != 0 && _scroller is null) {
                _scroller = StartCoroutine(StartScrolling(scrollVelocity, maxScroll));
            }

            // calculate target index
            float minDelta = float.PositiveInfinity;
            float alignedY = 0;
            int targetIndex = 0;
            int originalIndex = _listEntries.IndexOf(targetEntry);

            int lastIndex = _listEntries.Count - 1;
            float listEndY = _listEntries[lastIndex].transform.position.y;
            listEndY -= _listEntries[lastIndex].RectTransform.rect.height * _listEntries[lastIndex].transform.lossyScale.y;
            for (int i = 0; i <= _listEntries.Count; i++) {
                float y = i > lastIndex ? listEndY : _listEntries[i].transform.position.y;
                float delta = Mathf.Abs(y - targetPosition.y);
                if (delta < minDelta) {
                    minDelta = delta;
                    alignedY = y;
                    targetIndex = i;
                }
            }

            // effectively no reorder
            if (targetIndex == originalIndex || targetIndex == originalIndex + 1) {
                _itemReorderIndicator.gameObject.SetActive(false);
                return -1;
            }

            // all ok, show target location
            _itemReorderIndicator.gameObject.SetActive(indicateTargetPosition);
            _itemReorderIndicator.position = new Vector3(_itemReorderIndicator.position.x, alignedY);

            if (targetIndex > originalIndex) targetIndex--;
            // check we don't displace locked entries
            bool legalReorder = !_listEntries[targetIndex].Locked;
            UnityCore.Utility.TintAllGraphics(_itemReorderIndicator.gameObject, legalReorder ? _legalReorderColor : _illegalReorderColor);
            return legalReorder ? targetIndex : -1;
        }

        private void OnItemDrag(ListEntryBase entry, Vector2 pointer) {
            ResolveReorder(entry, pointer, indicateTargetPosition: true);
        }

        private void OnItemDragDrop(ListEntryBase entry, Vector2 pointer) {
            int targetIndex = ResolveReorder(entry, pointer, indicateTargetPosition: false);
            if (targetIndex < 0) return;

            // wizard-grade algorithm that ensures (i think?) that all locked entries do not change indices
            List<ListEntryBase> newOrder = new List<ListEntryBase>(_listEntries);
            newOrder.Remove(entry);
            newOrder.Insert(targetIndex, entry);
            newOrder.RemoveAll(x => x.Locked);

            for (int i = 0; i < _listEntries.Count; i++) {
                if (!_listEntries[i].Locked) continue;
                
                newOrder.Insert(i, _listEntries[i]);
            }

            for (int i = 0; i < _listEntries.Count; i++) {
                newOrder[i].RectTransform.SetSiblingIndex(i);
            }

            _listEntries = newOrder;
            ItemsChanged?.Invoke(Items);
        }

        private void OnItemRemove(ListEntryBase entry) {
            int index = _listEntries.IndexOf(entry);
            _listEntries.RemoveAt(index);
            Destroy(entry.gameObject);

            for (int i = index; i < _listEntries.Count; i++)
                _listEntries[i].OnIndexChanged(i);

            ItemsChanged?.Invoke(Items);
        }

        private void Awake()
        {
            _collapseToggle.onValueChanged.AddListener(OnCollapseToggleClicked);
            _addItemButton.Click += OnAddItemClicked;
            ItemsChanged += OnItemsChanged;
            SetItemOptions(_itemTypes);
            OnItemsChanged(Items);
        }

        private void OnDestroy() {
            _collapseToggle.onValueChanged.RemoveListener(OnCollapseToggleClicked);
            _addItemButton.Click -= OnAddItemClicked;
            _itemTypesDropdown.SelectedValueChanged -= AddItemByIndex;
        }

        private void OnAddItemClicked() { _itemTypesDropdown.WrappedDropdown.Show(); _itemTypesDropdown.SelectedValue = -1; }
    }
}