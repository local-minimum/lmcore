using UnityEngine;

public class HoverShiftUIPivot : MonoBehaviour
{
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

    private void Start()
    {
        if (arcItem)
        {
            neutralClaim = arcItem.progressClaimed;
        }
        neutralPivot = GetComponent<RectTransform>().pivot;
    }

    float easingStartTime;
    float easingStart;
    float easingEnd;
    bool isEasing;

    float EaseProgress => Mathf.Clamp01((Time.timeSinceLevelLoad - easingStartTime) / easeDuration);

    int defaultChildIndex;
    public void PointerEnter()
    {
        easingStartTime = Time.timeSinceLevelLoad;
        easingStart = 0;
        easingEnd = 1;

        if (arcItem) arcItem.progressClaimed = neutralClaim * hoverClaimSpaceFactor;

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

        isEasing = true;
    }

    public void PointerExit()
    {
        easingStartTime = Time.timeSinceLevelLoad;
        easingStart = EaseProgress;
        easingEnd = 0;

        if (arcItem) arcItem.progressClaimed = neutralClaim;

        if (BringForward)
        {
            arcItem.transform.SetSiblingIndex(defaultChildIndex);
        }

        isEasing = true;
    }

    private void Update()
    {
        if (!isEasing || target == null) return;

        var progress = EaseProgress;

        target.pivot = Vector2.Lerp(neutralPivot, hoverPivot, Mathf.Lerp(easingStart, easingEnd, progress));

        if (progress == 1)
        {
            isEasing = false;
        }
    }
}
