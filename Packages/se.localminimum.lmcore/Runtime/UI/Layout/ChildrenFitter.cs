using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ChildrenFitter : AbsFitter, ILayoutController
    {
        override public event RecalculateSizeEvent OnRecalculateSize;

        [SerializeField]
        Dimension _dimensions;
        override public Dimension Dimensions => _dimensions;

        [SerializeField]
        Vector2 padding;

        [SerializeField]
        List<RectTransform> Ignore = new List<RectTransform>();

        public void SetLayoutHorizontal()
        {
            if (!Dimensions.HasFlag(Dimension.Width)) return;

            Recalculate();
        }

        public void SetLayoutVertical()
        {
            if (!Dimensions.HasFlag(Dimension.Height)) return;
            Recalculate();
        }

        Rect ChildrenBounds
        {
            get
            {
                var bounds = new Rect();
                bool first = true;
                for (int i = 0, l = transform.childCount; i < l; i++)
                {
                    var child = transform.GetChild(i) as RectTransform;
                    if (!child.gameObject.activeSelf || Ignore.Contains(child)) continue;

                    var childBounds = child.rect;

                    var min = transform.InverseTransformPoint(child.TransformPoint(childBounds.min));
                    var max = transform.InverseTransformPoint(child.TransformPoint(childBounds.max));

                    // Debug.Log($"Considering {name}: {child} {childBounds}");
                    if (first)
                    {
                        bounds.min = min;
                        bounds.max = max;
                        first = false;
                    }
                    else
                    {
                        bounds.min = new Vector2(Mathf.Min(bounds.xMin, min.x), Mathf.Min(bounds.yMin, min.y));
                        bounds.max = new Vector2(Mathf.Max(bounds.xMax, max.x), Mathf.Max(bounds.yMax, max.y));
                    }

                }

                return bounds;
            }
        }

        Vector2 CalculateDelta()
        {
            var rt = transform as RectTransform;
            var bounds = ChildrenBounds;
            var newDelta = new Vector2(
                Dimensions.HasFlag(Dimension.Width) ? bounds.width : rt.sizeDelta.x,
                Dimensions.HasFlag(Dimension.Height) ? bounds.height : rt.sizeDelta.y);

            if (Dimensions.HasFlag(Dimension.Height))
            {
                newDelta.y += padding.y;
            }
            if (Dimensions.HasFlag(Dimension.Width))
            {
                newDelta.x += padding.x;
            }

            return newDelta;
        }

        [ContextMenu("Sync")]
        override public void Recalculate()
        {
            if (!enabled) return;

            var newDelta = CalculateDelta();

            var rt = transform as RectTransform;
            // Debug.Log($"{name}: New delta is {newDelta} was {rt.sizeDelta}");

            if (rt.sizeDelta != newDelta)
            {
                rt.sizeDelta = newDelta;
                OnRecalculateSize?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject != this.gameObject) return;
#endif

            /*
            var bounds = new Rect();
            bool first = true;
            for (int i = 0, l = transform.childCount; i < l; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (!child.gameObject.activeSelf || Ignore.Contains(child)) continue;

                var childBounds = child.rect;

                var min = transform.InverseTransformPoint(child.TransformPoint(childBounds.min));
                var max = transform.InverseTransformPoint(child.TransformPoint(childBounds.max));

                Debug.Log($"Considering {name}: {child} {childBounds}");
                if (first)
                {
                    bounds.min = min;
                    bounds.max = max;
                    first = false;
                } else
                {
                    bounds.min = new Vector2(Mathf.Min(bounds.xMin, min.x), Mathf.Min(bounds.yMin, min.y));
                    bounds.max = new Vector2(Mathf.Max(bounds.xMax, max.x), Mathf.Max(bounds.yMax, max.y));
                }

                var c = transform.TransformPoint(bounds.center);
                var s = transform.TransformVector(bounds.size - new Vector2(1, 1));
                Gizmos.color = Color.Lerp(Color.blue, Color.red, (float)i / (l - 1));
                Gizmos.DrawWireCube(c, s);
            }
            */

            var childrenBounds = ChildrenBounds;
            Gizmos.color = Color.cyan;
            var center = transform.TransformPoint(childrenBounds.center);
            var size = transform.TransformVector(childrenBounds.size);
            Gizmos.DrawWireCube(center, size);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(center, transform.TransformVector(CalculateDelta()));
        }
    }
}
