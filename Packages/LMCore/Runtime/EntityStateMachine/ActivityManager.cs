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
            // We don't handle stay taxation here at the moment because we
            // don't want to manage when it can be applied like if entity has 
            // moved or player has and such things.
            ActivityState.OnEnterState += AbsState_OnEnterState;
            ActivityState.OnExitState += AbsState_OnExitState;
        }

        private void OnDisable()
        {
            ActivityState.OnEnterState -= AbsState_OnEnterState;
            ActivityState.OnExitState -= AbsState_OnExitState;
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

        public void CheckTransition()
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
