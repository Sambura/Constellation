using System;
using UnityEngine;

namespace ConstellationUI
{
    public class ListEntryBase : LabeledUIElement
    {
        protected object Data;
        protected int Index;

        /// <summary>
        /// Implementation-specific data stored in the list entry. Usually it contains the data necessary
        /// for initializing list entry's UI. E.g. Data can be a struct with an entry name stored there,
        /// which is used by the subclass to initialize a UI Label. It can also be data for entry's icon,
        /// text color, click sound, etc.
        /// </summary>
        public virtual object EntryData { get => Data; set => Data = value; }
        /// <summary>
        /// Index of this entry in the list
        /// </summary>
        public virtual int EntryIndex { get => Index; set => Index = value; }
        /// <summary>
        /// If true - element cannot be deleted, reordered, or interacted with
        /// </summary>
        public virtual bool Locked { get; set; }
        public virtual RectTransform Container => null;
        /// <summary>
        /// Emitted when this item wants to get destroyed and removed from the list. Actual destruction
        /// should be done by the list master.
        /// </summary>
        public event Action RemoveItem;
        public event Action DragStart;
        /// <summary>
        /// Parameter - pointer location?
        /// </summary>
        public event Action<Vector2> Drag;
        public event Action<Vector2> DragDrop;
        /// <summary>
        /// Emitted when this item wants to create a new item, data and index for new item are provided
        /// </summary>
        public event Action<object, int> ItemCreationRequest;
        public event Action ItemChanged;

        /// <summary>
        /// Initialize this item and set its initial data
        /// </summary>
        public virtual void Initialize(object data, int index, Type dataType = null) { EntryData = data; EntryIndex = index; }
        /// <summary>
        /// List master should call this each time item's index changes
        /// </summary>
        public virtual void OnIndexChanged(int index) { EntryIndex = index; }

        protected void EmitItemRemoved() { RemoveItem?.Invoke(); }
        protected void EmitItemChanged() { ItemChanged?.Invoke(); }
        protected void EmitDrag(Vector2 pos) { Drag?.Invoke(pos); }
        protected void EmitDragDrop(Vector2 pos) { DragDrop?.Invoke(pos); }
        protected void EmitDragStart() { DragStart?.Invoke(); }
        protected void EmitItemCreation(object data, int index) { ItemCreationRequest?.Invoke(data, index); }
    }
}
