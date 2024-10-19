using System;
using UnityEngine;

namespace LMCore.Juice
{
    public delegate void EaseCompleteEvent(bool isAtEnd);

    [Serializable]
    public class TemporalEasing<T>
    {
        public event EaseCompleteEvent OnEaseComplete;

        public T StartValue;
        public T EndValue;

        public float fullEaseDuration;

        [SerializeField]
        AnimationCurve easing;

        float easeStart;
        public bool IsEasing { get; private set; } = false;

        float progressStart;
        float progressEnd;
        float duration;

        public void EaseStartToEnd()
        {
            if (IsEasing) { return; }

            progressStart = 0;
            progressEnd = 1;
            easeStart = Time.timeSinceLevelLoad;
            IsEasing = true;
            duration = fullEaseDuration;
        }

        public void EaseEndToStart()
        {
            if (IsEasing) { return; }

            progressStart = 1;
            progressEnd = 0;
            easeStart = Time.timeSinceLevelLoad;
            IsEasing = true;
            duration = fullEaseDuration;
        }

        public void EaseStartToEnd(float startProgress)
        {
            if (IsEasing) { return; }

            progressStart = startProgress;
            progressEnd = 1;
            easeStart = Time.timeSinceLevelLoad;
            IsEasing = true;
            duration = fullEaseDuration - progressStart * duration;
        }

        
        /// <summary>
        /// Stop animation and set progression to 1
        /// </summary>
        public void AbortEase()
        {
            if (!IsEasing) { return; }
            easeStart = Time.timeSinceLevelLoad - duration;
            IsEasing = false;
        }

        public void ReverseEase()
        {
            if (!IsEasing) { return; }

            progressStart = Progress;
            progressEnd = progressEnd == 0 ? 1 : 0;
            easeStart = Time.timeSinceLevelLoad;
            duration = fullEaseDuration * Mathf.Abs(progressEnd - progressStart);
            IsEasing = duration == 0 ? false : true;
        }

        private float TimeProgress => Mathf.Clamp01((Time.timeSinceLevelLoad - easeStart) / duration);
        public float Progress => Mathf.Lerp(progressStart, progressEnd, TimeProgress);

        public T EvaluateEnd() => Evaluate(1);

        public T Evaluate() => Evaluate(Progress, true);

        public T Evaluate(float p, bool canCompleteEase = false)
        {
            var progress = easing.Evaluate(p);

            var t = typeof(T);

            if (canCompleteEase && IsEasing && TimeProgress == 1)
            {
                IsEasing = false;
                OnEaseComplete?.Invoke(progress == 1);
            }

            if (t == typeof(Vector2))
            {
                return (T)Convert.ChangeType(
                    Vector2.Lerp(
                    (Vector2)Convert.ChangeType(StartValue, t),
                    (Vector2)Convert.ChangeType(EndValue, t),
                    progress
                ), t);
            }
            if (t == typeof(Vector3))
            {
                return (T)Convert.ChangeType(
                    Vector3.Lerp(
                    (Vector3)Convert.ChangeType(StartValue, t),
                    (Vector3)Convert.ChangeType(EndValue, t),
                    progress
                ), t);
            }
            if (t == typeof(float))
            {
                return (T)Convert.ChangeType(
                    Mathf.Lerp(
                        (float)Convert.ChangeType(StartValue, t),
                        (float)Convert.ChangeType(EndValue, t),
                        progress
                    ),
                    t
                );
            }

            return progress == 1 ? EndValue : StartValue;
        }
    }
}
