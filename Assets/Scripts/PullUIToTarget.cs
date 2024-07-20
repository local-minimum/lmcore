using LMCore.Extensions;
using LMCore.Juice;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PullUIToTarget : MonoBehaviour
{
    Transform originalParent;

    [SerializeField]
    RectTransform scaleReferenceRect;

    [SerializeField]
    bool OrderBeforeTarget = true;

    [SerializeField]
    List<MonoBehaviour> managedBehaviours = new List<MonoBehaviour>();

    List<MonoBehaviour> toggledBehaviours = new List<MonoBehaviour>();

    [SerializeField]
    RectTransform target;

    public RectTransform Target => target;

    // TODO: This doesn't work
    // [SerializeField, Range(0, 10)]
    int bonusRotationLaps = 0;

    [SerializeField]
    TemporalEasing<float> rotationEasing = new TemporalEasing<float>();

    [SerializeField]
    TemporalEasing<float> positionEasing = new TemporalEasing<float>();

    Quaternion rotationStart;
    Quaternion rotationEnd;
    float neededRotationAngle;
    float fullRotation;

    public float EaseDuration => Mathf.Max(positionEasing.fullEaseDuration, rotationEasing.fullEaseDuration);
    public bool Pulled { get; private set; } = false;
    public bool Pulling => rotationEasing.IsEasing || positionEasing.IsEasing;

    void SetupRotationEasing()
    {
        rotationStart = transform.rotation;
        var rotationForward = transform.forward;

        rotationEnd = Quaternion.LookRotation(rotationForward, target.up);

        neededRotationAngle = Quaternion.Angle(rotationStart, rotationEnd);
        fullRotation = neededRotationAngle + bonusRotationLaps * 360;
        if (neededRotationAngle == 0)
        {
            neededRotationAngle = fullRotation;
        }
        
        if (fullRotation > 0)
        {
            rotationEasing.EaseStartToEnd();
        }
    }

    Vector3 startLocalPosition;
    Vector3 localScaleBeforeClaim;
    Vector3 localScaleStart;
    Vector3 localScaleEnd;

    private void SetupPositionEasing()
    {
        startLocalPosition = transform.localPosition;
        localScaleStart = transform.localScale;

        var scaleVector = target.CalculateSize() / scaleReferenceRect.CalculateSize();
        var targetScaleFactor = Mathf.Min(scaleVector.x, scaleVector.y);
        localScaleEnd = localScaleStart * targetScaleFactor;

        if (startLocalPosition != target.localPosition) { positionEasing.EaseStartToEnd(); }
    }

    [ContextMenu("Pull to target")]
    public void PullSubject()
    {
        if (rotationEasing.IsEasing || positionEasing.IsEasing || Pulled) return;

        if (target == null)
        {
            Debug.LogError($"{name} lacks a pull target");
            return;
        }

        localScaleBeforeClaim = transform.localScale;

        toggledBehaviours = managedBehaviours.Where(mb => mb.enabled).ToList();
        foreach (MonoBehaviour mb in toggledBehaviours)
        {
            mb.enabled = false;
        }

        originalParent = transform.parent;
        transform.SetParent(target.parent, true);
        if (OrderBeforeTarget)
        {
            transform.SetSiblingIndex(target.GetSiblingIndex());
        }

        SetupRotationEasing();
        SetupPositionEasing();

        Pulled = true;
    }

    [ContextMenu("Restore parent")]
    public void RestoreParent()
    {
        if (!Pulled) return;

        transform.SetParent(originalParent, true);
        transform.localScale = localScaleBeforeClaim;

        foreach (MonoBehaviour mb in toggledBehaviours) { 
            mb.enabled = true; 
        }

        rotationEasing.ReverseEase();
        positionEasing.ReverseEase();

        toggledBehaviours.Clear();

        Pulled = false;
    }

    private void Update()
    {
        if (rotationEasing.IsEasing)
        {
            var progress = rotationEasing.Evaluate();
            var angle = fullRotation * progress;

            transform.rotation = Quaternion.SlerpUnclamped(rotationStart, rotationEnd, angle / neededRotationAngle);

            if (!rotationEasing.IsEasing)
            {
                Debug.Log(transform.rotation == rotationEnd);
                transform.rotation = rotationEnd;
            }
        }

        if (positionEasing.IsEasing)
        {
            var progress = positionEasing.Evaluate();
            transform.localPosition = Vector3.Lerp(startLocalPosition, target.localPosition, progress);
            transform.localScale = Vector3.Lerp(localScaleStart, localScaleEnd, progress);
            if (!positionEasing.IsEasing)
            {
                transform.localPosition = target.localPosition;
                transform.localScale = localScaleEnd;
            }
        }
    }
}
