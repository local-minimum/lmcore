using LMCore.Juice;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiledDungeon.Actions
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

        System.Action OnDoneCallback;

        public override void Finalise()
        {
            easing.AbortEase();
            ApplyRotation(easing.Evaluate());
            OnDoneCallback?.Invoke();
            OnDoneCallback = null;
        }

        public override void Play(Action onDoneCallback)
        {
            OnDoneCallback = onDoneCallback;
            abandonned = false;
            easing.EaseStartToEnd();
        }

        public override void PlayFromCurrentProgress(Action onDoneCallback)
        {
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
            } else if (OnDoneCallback != null)
            {
                OnDoneCallback();
                OnDoneCallback = null;
            }
        }
    }
}
