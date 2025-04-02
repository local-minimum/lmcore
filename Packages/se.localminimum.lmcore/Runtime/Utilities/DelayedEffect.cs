using UnityEngine;

namespace LMCore.Utilities
{
    /// <summary>
    /// Helper class that encapsulates executing code after a certain
    /// amount of time without needing co-routines and allows for
    /// extending the delay.
    /// </summary>
    public class DelayedEffect
    {
        readonly System.Action onComplete;
        readonly System.Action<float> onProgress;

        readonly TimerType timerType;

        readonly float startTime;
        float endTime;


        /// <summary>
        /// If the on complete action has been executed
        /// </summary>
        public bool Completed { get; private set; } = false;

        float Now => timerType == TimerType.RealTime ? Time.realtimeSinceStartup : Time.timeSinceLevelLoad;

        /// <summary>
        /// Progress on delay since effect was created on a 0 - 1 scale
        /// </summary>
        public float Progress => Mathf.Clamp01((Now - startTime) / (endTime - startTime));

        public DelayedEffect(
            float delay,
            System.Action onComplete,
            TimerType timerType = TimerType.GameTime,
            System.Action<float> onProgress = null)
        {
            Completed = false;

            // We must set the time type before accessing now!
            this.timerType = timerType;

            startTime = Now;
            endTime = delay + Now;

            this.onComplete = onComplete;
            this.onProgress = onProgress;

        }

        /// <summary>
        /// Time remaining until completion in seconds
        /// </summary>
        public float RemainingTime => Completed ? 0 : Mathf.Max(0, endTime - Now);

        /// <summary>
        /// Checks if it is time to execute the oncomplete action and executes
        /// it if it's time to do so.
        /// 
        /// Returns if the delayed effect is completed
        /// </summary>
        public bool Check()
        {
            if (Completed) return true;

            onProgress?.Invoke(Progress);

            if (RemainingTime == 0)
            {
                Completed = true;
                onComplete();
            }

            return Completed;
        }

        /// <summary>
        /// Executes the on complete if it hasn't been already
        /// even though it may not be time to do so yet
        /// </summary>
        public void ForceComplete()
        {
            if (Completed) return;

            Completed = true;

            Debug.Log($"{this} got force completed");

            onProgress?.Invoke(1);
            onComplete();
        }

        /// <summary>
        /// Extend or shorten the duration of the delay with supplied value
        /// </summary>
        public bool AmmendDelay(float delayChange)
        {
            if (Completed) return false;

            endTime += delayChange;
            Check();

            return true;
        }

        public override string ToString() =>
            $"<DelayedEffect{(onProgress == null ? "" : " with Progress")}: {RemainingTime} remain at {Progress * 100:F2}%{(Completed ? " Completed" : "")}>";
    }
}
