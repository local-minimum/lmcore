using LMCore.Extensions;
using LMCore.Juice;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.UI
{
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

        public RectTransform Target { get; set; }

        [SerializeField]
        TemporalEasing<float> rotationEasing = new TemporalEasing<float>();

        [SerializeField]
        TemporalEasing<float> positionEasing = new TemporalEasing<float>();

        Quaternion rotationStart;
        Quaternion rotationEnd;
        float neededRotationAngle;

        protected string PrefixLogMessage(string message) =>
            $"PullUI Pulled({Pulled}) Pulling({Pulling}): {message}";

        public float EaseDuration => Mathf.Max(positionEasing.fullEaseDuration, rotationEasing.fullEaseDuration);
        public bool Pulled { get; set; } = false;
        public bool Pulling => rotationEasing.IsEasing || positionEasing.IsEasing;

        void SetupRotationEasing()
        {
            rotationStart = transform.rotation;
            var rotationForward = transform.forward;

            rotationEnd = Quaternion.LookRotation(rotationForward, Target.up);

            neededRotationAngle = Quaternion.Angle(rotationStart, rotationEnd);

            if (neededRotationAngle != 0)
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
            //Debug.Log(PrefixLogMessage($"Setting origin to {startLocalPosition} (scale {localScaleStart})"));

            var scaleVector = Target.CalculateSize() / scaleReferenceRect.CalculateSize();
            var targetScaleFactor = Mathf.Min(scaleVector.x, scaleVector.y);
            localScaleEnd = localScaleStart * targetScaleFactor;

            if (startLocalPosition != Target.localPosition) { positionEasing.EaseStartToEnd(); }
        }

        [ContextMenu("Pull to target")]
        public void PullSubject()
        {
            if (rotationEasing.IsEasing || positionEasing.IsEasing || Pulled)
            {
                Debug.LogWarning(PrefixLogMessage($"Refused pulling because " +
                    $"rotating({rotationEasing.IsEasing}) " +
                    $"translating({positionEasing.IsEasing} " +
                    $"being pulled({Pulled})"));
                return;
            }

            if (Target == null)
            {
                Debug.LogError(PrefixLogMessage($"{name} lacks a pull target"));
                return;
            }

            localScaleBeforeClaim = transform.localScale;

            toggledBehaviours = managedBehaviours.Where(mb => mb.enabled).ToList();
            foreach (MonoBehaviour mb in toggledBehaviours)
            {
                mb.enabled = false;
            }

            originalParent = transform.parent;
            transform.SetParent(Target.parent, true);
            if (OrderBeforeTarget)
            {
                transform.SetSiblingIndex(Target.GetSiblingIndex());
            }

            SetupRotationEasing();
            SetupPositionEasing();

            Pulled = true;
        }

        [ContextMenu("Restore parent")]
        public void EaseToOriginalTransform()
        {
            if (!Pulled) return;

            RestoreInitialSizeAndParent();

            foreach (MonoBehaviour mb in toggledBehaviours)
            {
                mb.enabled = true;
            }

            rotationEasing.ReverseEase();
            positionEasing.ReverseEase();

            toggledBehaviours.Clear();

            Pulled = false;
        }

        void RestoreInitialSizeAndParent()
        {
            transform.SetParent(originalParent);
            transform.localScale = localScaleBeforeClaim;
            Pulled = false;
        }

        private void Update()
        {
            if (rotationEasing.IsEasing)
            {
                var progress = rotationEasing.Evaluate();
                var angle = neededRotationAngle * progress;

                transform.rotation = Quaternion.SlerpUnclamped(rotationStart, rotationEnd, angle / neededRotationAngle);

                if (!rotationEasing.IsEasing)
                {
                    //Debug.Log(PrefixLogMessage($"{transform.rotation} == {rotationEnd}"));
                    transform.rotation = rotationEnd;
                }
            }

            if (positionEasing.IsEasing)
            {
                var progress = positionEasing.Evaluate();
                transform.localPosition = Vector3.Lerp(startLocalPosition, Target.localPosition, progress);
                transform.localScale = Vector3.Lerp(localScaleStart, localScaleEnd, progress);
                if (!positionEasing.IsEasing)
                {
                    transform.localPosition = Target.localPosition;
                    transform.localScale = localScaleEnd;
                }
            }
        }
    }
}
