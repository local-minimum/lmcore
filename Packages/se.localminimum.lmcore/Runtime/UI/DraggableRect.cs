using UnityEngine;
using UnityEngine.EventSystems;

namespace LMCore.UI
{
    public delegate void DragDeltaEvent(Vector2 delta);
    public delegate void BeforeDragEvent();
    public delegate void AfterDragEvent();

    [RequireComponent(typeof(RectTransform))]
    public class DraggableRect : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        /// <summary>
        /// Emitted right before dragging starts
        /// </summary>
        public event BeforeDragEvent OnBeforeDrag;

        /// <summary>
        /// Emitted right aftger dragging ends
        /// </summary>
        public event AfterDragEvent OnAfterDrag;

        /// <summary>
        /// The dragged distance every frame
        /// </summary>
        public event DragDeltaEvent OnDragDelta;

        DragManager _Manager;
        public DragManager Manager
        {
            get
            {
                if (_Manager == null)
                {
                    _Manager = GetComponentInParent<DragManager>();
                }

                return _Manager;
            }
        }

        bool dragging => Manager.Dragged == this;

        public RectTransform rectTransform => transform as RectTransform;

        #region Unity Drag Events
        public void OnBeginDrag(PointerEventData eventData) =>
            BeginDrag();

        public void BeginDrag()
        {
            if (Manager == null || !Manager.enabled)
            {
                Debug.LogWarning($"Dragging manager ({Manager.ToString() ?? "null"}) available for {this}");
                return;
            }

            Manager.Manage(this);
            OnBeforeDrag?.Invoke();
        }

        public void OnDrag(PointerEventData eventData) =>
            Drag(eventData.delta);

        public void Drag(Vector2 delta)
        {
            if (!dragging)
            {
                Debug.LogError($"Draggable rect {name} is not being dragged");
                return;
            }

            OnDragDelta?.Invoke(delta);
        }

        public void OnEndDrag(PointerEventData eventData) =>
            EndDrag();

        public void EndDrag()
        {
            if (!dragging) return;

            Manager.Release(this);
            OnAfterDrag?.Invoke();
        }
        #endregion
    }
}
