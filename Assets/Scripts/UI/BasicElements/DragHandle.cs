using UnityEngine;
using UnityEngine.EventSystems;

namespace ConstellationUI
{
    public class DragHandle : MonoDraggable, IPointerClickHandler, IDragHandler
    {
        public event System.Action<Vector2> Displace;
        public event System.Action<PointerEventData> Click;

        // This is needed to mask drag events from the parent scroll rect :)
        public void OnDrag(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData) {
            Click?.Invoke(eventData);
        }

        public override void SetPositionWithoutNotify(Vector3 position)
        {
            Vector3 oldPosition = transform.position;
            Displace?.Invoke(position - oldPosition);
        }
    }
}