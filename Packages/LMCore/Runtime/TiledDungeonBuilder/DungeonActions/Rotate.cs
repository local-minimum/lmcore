using LMCore.Juice;
using System;
using UnityEngine;

namespace LMCore.TiledDungeon.Actions
{
    public class Rotate : AbstractDungeonAction 
    {
        [SerializeField]
        Transform[] targets;

        [SerializeField, Tooltip("This should be in local euler angles")]
        TemporalEasing<Vector3> easing;

        public override bool Available => targets.Length > 0;

        bool abandonned = false;
        public override bool IsEasing => !abandonned && easing.IsEasing;

        void ApplyRotation(Vector3 euler)
        {
            var rotation = Quaternion.Euler(euler);

            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].transform.localRotation = rotation;
            }
        }

        public override void Abandon()
        {
            if (abandonned) { return; }

            abandonned = true;
            easing.AbortEase();
            ApplyRotation(easing.Evaluate());
        }

        Action OnDoneCallback;
        Action<float> OnProgressCallback;

        public override void Finalise(bool invokeEvents)
        {
            easing.AbortEase();
            ApplyRotation(easing.Evaluate());

            if (invokeEvents)
            {
                OnProgressCallback?.Invoke(1f);
                OnDoneCallback?.Invoke();
            }

            OnDoneCallback = null;
            OnProgressCallback = null;
        }

        public override void Play(Action onDoneCallback = null, Action<float> onProgressCallback = null)
        {
            OnProgressCallback = onProgressCallback;
            OnDoneCallback = onDoneCallback;
            abandonned = false;
            easing.EaseStartToEnd();
        }

        public override void PlayFromCurrentProgress(Action onDoneCallback = null, Action<float> onProgressCallback = null)
        {
            OnProgressCallback = onProgressCallback;
            OnDoneCallback = onDoneCallback;            

            float startProgress = NewtonRaphson(
                (progress) => Vector3.SqrMagnitude(transform.localEulerAngles - easing.Evaluate(progress, false)),
                0.0001f
            );

            abandonned = false;
            easing.EaseStartToEnd(startProgress);
        }
        private void Update()
        {
            if (abandonned) { return; }

            if (easing.IsEasing)
            {
                ApplyRotation(easing.Evaluate());
                OnProgressCallback?.Invoke(easing.Progress);
            }
            else
            {
                if (OnProgressCallback != null)
                {
                    OnProgressCallback(1f);
                    OnProgressCallback = null;
                }

                if (OnDoneCallback != null)
                {
                    OnDoneCallback();
                    OnDoneCallback = null;
                }
            }
        }
    }
}
