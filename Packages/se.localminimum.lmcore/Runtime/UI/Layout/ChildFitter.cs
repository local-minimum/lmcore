using UnityEngine;

namespace LMCore.UI
{

    [RequireComponent(typeof(RectTransform))]
    public class ChildFitter : AbsFitter
    {
        override public event RecalculateSizeEvent OnRecalculateSize;

        [SerializeField]
        bool liveFitting;

        [SerializeField]
        Dimension dimension;
        override public Dimension Dimensions => dimension;

        [SerializeField]
        RectTransform Child;

        [SerializeField]
        Vector2 minSize = Vector2.zero;

        [SerializeField]
        Vector2 fixedPadding;

        bool inited = false;
        private void OnValidate()
        {
            if (Child == null) return;

            if (!inited)
            {
                if (Child != null)
                {
                    if (Child.GetComponent<ChildFitterSentinel>() == null)
                    {
                        Child.gameObject.AddComponent<ChildFitterSentinel>();
                    }
                }
                inited = true;
            }

            Child.GetComponent<ChildFitterSentinel>().SetDirty();
        }

        [ContextMenu("Recalculate")]
        /// <summary>
        /// Force recalculate size
        /// </summary>
        override public void Recalculate()
        {
            if (!enabled || Child == null || dimension == Dimension.Nothing)
            {
                return;
            }

            var rt = transform as RectTransform;
            var myDelta = rt.sizeDelta;
            var childDelta = Child.rect.size;
            // Debug.Log($"{name}: Raw child size: {childDelta} compared to my {myDelta}");

            if (dimension.HasFlag(Dimension.Width))
            {
                childDelta.x += fixedPadding.x * 2;
                myDelta.x = Mathf.Max(childDelta.x, minSize.x);
            }
            if (dimension.HasFlag(Dimension.Height))
            {
                childDelta.y += fixedPadding.y * 2;
                myDelta.y = Mathf.Max(childDelta.y, minSize.y);
            }

            if (myDelta != rt.sizeDelta)
            {
                // Debug.Log($"{name}: My adjusted delta {myDelta} vs {rt.sizeDelta}");
                rt.sizeDelta = myDelta;

                OnRecalculateSize?.Invoke();
            }
        }

        private void Update()
        {
            if (liveFitting) Recalculate();
        }
    }
}
