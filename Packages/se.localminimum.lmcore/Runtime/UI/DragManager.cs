using UnityEngine;

namespace LMCore.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class DragManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Parent of rects while dragged, if empty drag manager becomes parent.")]
        Transform dragLayer;

        /// <summary>
        /// The transform that will parent the dragged rect
        /// </summary>
        Transform DragLayer => dragLayer ?? transform;

        Transform _draggedRectOriginalParent;

        /// <summary>
        /// The parent of the dragged rect before it got dragged
        /// </summary>
        public Transform DraggedRectOriginParent => _draggedRectOriginalParent;

        Vector2 _draggedOrigin;
        DraggableRect _draggedRect;

        /// <summary>
        /// The currently dragged rect
        /// </summary>
        public DraggableRect Dragged => _draggedRect;

        public bool Dragging => _draggedRect != null;

        /// <summary>
        /// Start dragging a rect
        /// </summary>
        public void Manage(DraggableRect drag)
        {
            // In case previous rect wasn't released, we force release
            // it first
            ReturnDraggedRect();

            _draggedRect = drag;
            _draggedRectOriginalParent = drag.transform.parent;
            _draggedOrigin = drag.rectTransform.anchoredPosition;

            drag.transform.SetParent(DragLayer);
            drag.OnDragDelta += Drag_OnDragDelta;

            Debug.Log($"Dragging started for {drag}");
        }

        private void Drag_OnDragDelta(Vector2 delta)
        {
            _draggedRect.rectTransform.Translate(delta);
        }


        RectTransform _target;
        Vector3 targetOffset;

        public void DragTo(RectTransform target, Vector2 offset)
        {
            this._target = target;
            targetOffset = offset;
        }

        /// <summary>
        /// Release dragging of rect
        /// </summary>
        public void Release(DraggableRect drag)
        {
            if (drag != _draggedRect)
            {
                // The should already have been removed but lets be sure
                drag.OnDragDelta -= Drag_OnDragDelta;
                return;
            }

            ReturnDraggedRect();
        }

        private void OnDisable()
        {
            ReturnDraggedRect();
        }

        void ReturnDraggedRect()
        {
            if (_draggedRect == null) return;

            _draggedRect.OnDragDelta -= Drag_OnDragDelta;
            _draggedRect.transform.SetParent(_draggedRectOriginalParent);
            _draggedRect.rectTransform.anchoredPosition = _draggedOrigin;

            Debug.Log($"Dragging ended for {_draggedRect}");
            _draggedRect = null;
        }

        private void Update()
        {
            if (_target)
            {
                if (_draggedRect == null)
                {
                    _target = null;
                    return;
                }

                var targetPos = _target.position + targetOffset;

                _draggedRect.transform.position = Vector3.Lerp(
                    _draggedRect.transform.position,
                    targetPos,
                    Mathf.Max(0.25f, 0.5f - Time.deltaTime));

                if ((_draggedRect.transform.position - targetPos).sqrMagnitude < 0.1f)
                {
                    _draggedRect.transform.position = targetPos;
                    _target = null;
                }
            }
        }
    }
}
