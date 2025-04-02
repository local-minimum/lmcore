using UnityEngine;

namespace LMCore.Utilities
{
    public struct DurationProgress
    {
        public enum TimeMode { RealTimeSinceStartup, GameTimeLevelLoad };

        public float StartTime { get; private set; }
        public float Duration { get; private set; }

        public TimeMode Mode { get; private set; }

        public float Elapsed =>
            GetTime(Mode) - StartTime;

        /// <summary>
        /// A zero duration isn't a valid duration progress
        /// </summary>
        public bool Valid => Duration > 0;

        /// <summary>
        /// If there's passed more or equal time to the duration
        /// </summary>
        public readonly bool Completed => Duration <= 0 || Elapsed >= Duration;

        /// <summary>
        /// The 0 - 1 progress on the duration
        /// </summary>
        public readonly float Progress => Completed ? 1f : Elapsed / Duration;

        private static float GetTime(TimeMode mode)
        {
            switch (mode)
            {
                case TimeMode.GameTimeLevelLoad:
                    return Time.timeSinceLevelLoad;
                case TimeMode.RealTimeSinceStartup:
                    return Time.realtimeSinceStartup;
                default:
                    return Time.timeSinceLevelLoad;
            }
        }

        public DurationProgress(float duration, TimeMode mode = TimeMode.GameTimeLevelLoad)
        {
            Mode = mode;
            StartTime = GetTime(mode);
            Duration = Mathf.Max(0, duration);
        }

        public override string ToString() =>
                $"<DurationProgress {(Valid ? "Valid" : "Invalid")}{(Completed ? " Completed" : "")}: {Elapsed:F2}s ({Progress * 100f:F2}%) of {Duration}s>";

    };
}
