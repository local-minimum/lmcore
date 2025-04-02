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

        bool _Disabled;
        public bool Disabled
        {
            get
            {
                return _Disabled;
            }

            set
            {
                if (!value)
                {
                    Abort();
                }
                else
                {
                    OnProgress?.Invoke(0);
                }
                _Disabled = value;
                enabled = !value;
            }
        }

        [SerializeField]
        private float activationDuration = 1f;

        [SerializeField]
        private bool disableOnComplete = true;

        [SerializeField]
        private bool resetProgressOnComplete;

        private bool animating;
        private float startTime;

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"DelayedEvent: Animating({animating}) Progress({Progress}) Disabled({Disabled})");
        }

        /// <summary>
        /// Initiate without argument used by EventTrigger
        /// </summary>
        public void Initiate() => Initiate(null);
        public void Initiate(UnityAction callback)
        {
            if (animating || Disabled) { return; }

            animating = true;
            startTime = Time.timeSinceLevelLoad;

            if (callback != null)
            {
                OnComplete.AddListener(callback);
            }
            OnStart?.Invoke();
        }

        /// <summary>
        /// Abort without argument used by EventTrigger
        /// </summary>
        public void Abort() => Abort(null);
        public void Abort(UnityAction removeCompleteCallback)
        {
            if (!animating || Disabled) { return; }

            animating = false;

            OnProgress?.Invoke(0);
            OnAbort?.Invoke();

            if (removeCompleteCallback != null)
            {
                OnComplete.RemoveListener(removeCompleteCallback);
            }

        }

        float Progress => Mathf.Clamp01((Time.timeSinceLevelLoad - startTime) / activationDuration);

        private void Start()
        {
            OnProgress?.Invoke(0);
        }

        private void Update()
        {
            if (!animating) { return; }

            var progress = Progress;
            OnProgress?.Invoke(progress);

            if (progress == 1)
            {
                animating = false;
                OnComplete?.Invoke();
                if (disableOnComplete)
                {
                    Disabled = true;
                }
                if (resetProgressOnComplete)
                {
                    progress = 0;
                    OnProgress?.Invoke(progress);
                }
            }
        }
    }
}
