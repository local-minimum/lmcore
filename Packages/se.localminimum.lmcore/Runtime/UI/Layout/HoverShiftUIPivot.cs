using UnityEngine;

namespace LMCore.UI
{
    public class HoverShiftUIPivot : MonoBehaviour
    {
        public enum Mode { None, Pivot, Shift, PivotAndShift };

        public Mode mode = Mode.PivotAndShift;

        [SerializeField]
        RectTransform target;

        [SerializeField]
        AnimationCurve easing;

        [SerializeField]
        float easeDuration = 0.3f;

        [SerializeField, Range(0, 3)]
        float hoverClaimSpaceFactor = 1.4f;

        [SerializeField]
        ArcItemUI arcItem;

        float neutralClaim;

        [SerializeField]
        Vector2 hoverPivot;

        Vector2 neutralPivot;

        [SerializeField, Tooltip("Item becomes first child when shifting")]
        bool BringForward = true;

        private void Awake()
        {
            neutralClaim = arcItem.progressClaimed;
            neutralPivot = GetComponent<RectTransform>().pivot;
        }

        float easingStartTime;
        float easingStart;
        float easingEnd;
        bool isEasing;


        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"HoverShift {name}: Easing({isEasing}) {easingStart} -> {easingEnd} " +
                $"Progress ({EaseProgress}) Neutral Claim ({neutralClaim})");
        }

        float EaseProgress => Mathf.Clamp01((Time.timeSinceLevelLoad - easingStartTime) / easeDuration);

        int defaultChildIndex;

        public bool Hovered { get; private set; }

        [ContextMenu("SimulateEnter")]
        public void PointerEnter()
        {
            if (mode == Mode.None || !enabled) return;

            Hovered = true;

            easingStartTime = Time.timeSinceLevelLoad;
            easingStart = 0;
            easingEnd = 1;

            if (mode == Mode.Shift || mode == Mode.PivotAndShift)
            {
                arcItem.progressClaimed = neutralClaim * hoverClaimSpaceFactor;
            }

            if (BringForward)
            {
                var parent = arcItem.transform.parent;
                for (int childIdx = 0; childIdx < parent.childCount; childIdx++)
                {
                    if (parent.GetChild(childIdx) == arcItem.gameObject)
                    {
                        defaultChildIndex = childIdx;
                        break;
                    }
                }

                arcItem.transform.SetAsLastSibling();
            }

            if (mode == Mode.Pivot || mode == Mode.PivotAndShift)
            {
                isEasing = true;
            }
        }

        [ContextMenu("SimulateExit")]
        public void PointerExit()
        {
            if (mode == Mode.None || !enabled) return;

            Hovered = false;

            easingStart = EaseProgress;
            easingEnd = 0;
            // Must set time last since it else interferes with ease progress
            easingStartTime = Time.timeSinceLevelLoad;


            if (mode == Mode.Shift || mode == Mode.PivotAndShift)
            {
                arcItem.progressClaimed = neutralClaim;
            }

            if (BringForward)
            {
                arcItem.transform.SetSiblingIndex(defaultChildIndex);
            }

            if (mode == Mode.Pivot || mode == Mode.PivotAndShift)
            {
                isEasing = true;
            }
        }

        private void Update()
        {
            if (!isEasing || target == null) return;

            var progress = EaseProgress;

            target.pivot = Vector2.Lerp(neutralPivot, hoverPivot, easing.Evaluate(Mathf.Lerp(easingStart, easingEnd, progress)));
            // target.pivot = Vector2.Lerp(neutralPivot, hoverPivot, Mathf.Lerp(easingStart, easingEnd, progress));

            if (progress == 1)
            {
                isEasing = false;
            }
        }
    }
}
