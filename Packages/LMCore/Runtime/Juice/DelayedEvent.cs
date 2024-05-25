using UnityEngine;
using UnityEngine.Events;

namespace LMCore.Juice
{
    public class DelayedEvent : MonoBehaviour
    {
        public UnityEvent OnStart;
        public UnityEvent OnAbort;
        public UnityEvent OnComplete;
        public UnityEvent<float> OnProgress;

        public bool Disabled;

        [SerializeField]
        private float activationDuration = 1f;

        [SerializeField]
        private bool disableOnComplete = true;

        private bool active;
        private float startTime;

        public void Initiate()
        {
            if (active || Disabled) { return; }

            active = true;
            startTime = Time.timeSinceLevelLoad;

            OnStart.Invoke();
        }

        public void Abort()
        {
            if (!active || Disabled) { return; }

            active = false;

            OnProgress.Invoke(0);
            OnAbort.Invoke();
        }

        float Progress => Mathf.Clamp01((Time.timeSinceLevelLoad - startTime) / activationDuration);

        private void Start()
        {
            OnProgress.Invoke(0);
        }

        private void Update()
        {
            if (!active) { return; }

            var progress = Progress;
            OnProgress.Invoke(progress);

            if (progress == 1)
            {
                active = false;
                OnComplete.Invoke();
                if (disableOnComplete)
                {
                    Disabled = true;
                }
            }
        }
    }
}
