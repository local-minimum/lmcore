using System;
using UnityEngine;

namespace LMCore.Utilities
{
    public enum TimerType { RealTime, GameTime };

    public class StateTimer<T> where T : IEquatable<T>
    {

        public T state { get; private set; }
        float switchTime;
        TimerType timeType;

        public StateTimer(T startState, TimerType timeType = TimerType.GameTime)
        {
            state = startState;
            this.timeType = timeType;
            switchTime = Now;
        }

        /// <summary>
        /// How long it has been in the current state
        /// </summary>
        public float Duration => Now - switchTime;

        float Now => timeType == TimerType.RealTime ?
            Time.realtimeSinceStartup :
            Time.timeSinceLevelLoad;

        /// <summary>
        /// Compares current state with the recorded state and emits
        /// the time it has been/was in the recorded state before this call.
        /// 
        /// Returns true if state changed
        /// </summary>
        /// <param name="currentState">The current state</param>
        /// <param name="duration">Duration that it has been/was in the recorded
        /// state before this call</param>
        /// <returns>If it shifted state</returns>
        public bool UpdateState(T currentState, out float duration)
        {
            duration = Now - switchTime;

            if ((state == null && currentState == null) ||
                (state != null && state.Equals(currentState)) ||
                (currentState != null && currentState.Equals(state)))
            {
                return false;
            }

            state = currentState;
            switchTime = Now;

            return true;
        }
    }
}
