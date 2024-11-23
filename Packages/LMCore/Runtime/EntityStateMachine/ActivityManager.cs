using LMCore.EntitySM.State;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.EntitySM
{
    public class ActivityManager : MonoBehaviour
    {
        [SerializeField]
        Personality personality;

        [SerializeField]
        StateType EntryState = StateType.Patrolling;

        [SerializeField]
        List<ActivityState> States = new List<ActivityState>();

        public ActivityState ActiveState { get; private set; }

        protected string PrefixLogMessage(string message) =>
            $"ActivityManager {name}: {message}";

        private void Start()
        {
            if (ActiveState == null) {
                ActiveState = States.FirstOrDefault(s => s.State == EntryState);

                if (ActiveState == null)
                {
                    ActiveState = States.FirstOrDefault();
                    Debug.LogError(PrefixLogMessage($"No {EntryState} configured, fell back on {ActiveState}"));
                }
            }
        }

        private void OnEnable()
        {
            ActivityState.OnEnterState += AbsState_OnEnterState;
            ActivityState.OnExitState += AbsState_OnExitState;
            ActivityState.OnStayState += AbsState_OnStayState;
        }

        private void OnDisable()
        {
            ActivityState.OnEnterState -= AbsState_OnEnterState;
            ActivityState.OnExitState -= AbsState_OnExitState;
            ActivityState.OnStayState -= AbsState_OnStayState;
        }

        private void AbsState_OnStayState(ActivityManager manager, ActivityState state)
        {
            // TODO: how to regulate when this can happen, probably only once per
            // move / tick
            if (manager == this)
            {
                state.TaxStayPersonality(personality);
            }
        }

        private void AbsState_OnExitState(ActivityManager manager, ActivityState state)
        {
            if (manager == this)
            {
                state.TaxExitPersonality(personality);
            }
        }

        private void AbsState_OnEnterState(ActivityManager manager, ActivityState state)
        {
            if (manager == this)
            {
                state.TaxEnterPersonality(personality);
            }
        }

        void CheckTransition()
        {
            if (ActiveState.CheckTransition(personality, out StateType newStateType))
            {
                var newState = States.FirstOrDefault(s => s.State == newStateType);
                if (newState == null)
                {
                    Debug.LogError(PrefixLogMessage($"{ActiveState} wanted to transition to {newStateType}, but we don't know it"));
                    return;
                }

                //Taxing personality is done by 
                ActiveState.Exit();
                ActiveState = newState;
                ActiveState.Enter();
            }
        }
    }
}
