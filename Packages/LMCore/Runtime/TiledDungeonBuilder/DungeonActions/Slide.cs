using LMCore.Juice;
using UnityEngine;

namespace LMCore.TiledDungeon.Actions {
    public class Slide : AbstractDungeonAction
    {
        [SerializeField, Tooltip("The node that the action is applied on")]
        Transform target;

        [SerializeField, Tooltip("This should be in local positions, not offset from initial position")]
        TemporalEasing<Vector3> easing;

        public override bool Available => target != null;

        bool abandonned = false;
        override public bool IsEasing => !abandonned && easing.IsEasing;

        void SyncSlide()
        {
            if (abandonned) { return; }

            target.transform.localPosition = easing.Evaluate();
        }

        public override void Abandon()
        {
            if (abandonned) return;

            abandonned = true;

            easing.AbortEase();
            SyncSlide();
        }

        System.Action OnDoneCallback;

        public override void Finalise()
        {
            easing.AbortEase();
            SyncSlide();
            OnDoneCallback?.Invoke();
            OnDoneCallback = null;
        }

        public override void Play(System.Action onDoneCallback)
        {
            OnDoneCallback = onDoneCallback;
            abandonned = false;
            easing.EaseStartToEnd();
        }

        public override void PlayFromCurrentProgress(System.Action onDoneCallback)
        {
            OnDoneCallback = onDoneCallback;

            float startProgress = NewtonRaphson(
                (progress) => Vector3.SqrMagnitude(transform.localPosition - easing.Evaluate(progress, false)),
                0.0001f
            );

            abandonned = false;
            easing.EaseStartToEnd(startProgress);
        }

        private void Update()
        {
            if (abandonned) { return ; }

            if (easing.IsEasing) 
            {
                SyncSlide();
            } else if (OnDoneCallback != null)
            {
                OnDoneCallback();
                OnDoneCallback = null;
            }
        }
    }
}
