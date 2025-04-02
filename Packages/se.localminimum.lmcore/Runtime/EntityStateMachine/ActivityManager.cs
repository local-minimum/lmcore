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
        public Personality Personality => personality;

        public StateType EntryState = StateType.Patrolling;

        [SerializeField]
        List<ActivityState> States = new List<ActivityState>();

        public ActivityState ActiveState { get; private set; }

        protected string PrefixLogMessage(string message) =>
            $"ActivityManager {name}: {message}";

        bool InitState()
        {
            if (ActiveState == null)
            {
                ActiveState = States.FirstOrDefault(s => s.State == EntryState);

                if (ActiveState == null)
                {
                    ActiveState = States.FirstOrDefault();
                    Debug.LogError(PrefixLogMessage($"No {EntryState} configured, fell back on {ActiveState}"));
                }

                if (ActiveState != null)
                {
                    ActiveState.Enter();
                    return true;
                }
            }

            return false;
        }

        private void OnEnable()
        {
            // We don't handle stay taxation here at the moment because we
            // don't want to manage when it can be applied like if entity has 
            // moved or player has and such things.
            ActivityState.OnEnterState += OnEnterState;
            ActivityState.OnExitState += OnExitState;
        }

        private void OnDisable()
        {
            ActivityState.OnEnterState -= OnEnterState;
            ActivityState.OnExitState -= OnExitState;
        }

        private void OnExitState(ActivityManager manager, ActivityState state)
        {
            if (manager == this)
            {
                state.TaxExitPersonality(personality);
            }
        }

        private void OnEnterState(ActivityManager manager, ActivityState state)
        {
            if (manager == this)
            {
                state.TaxEnterPersonality(personality);
            }
        }

        /// <summary>
        /// The activity manger has a state of the target type
        /// </summary>
        public bool HasState(StateType target) =>
            States.Any(s => s.State == target);

        /// <summary>
        /// The active state, if it exists, has a transition to the target type
        /// </summary>
        public bool HasTransition(StateType target) =>
            ActiveState != null && ActiveState.HasTransition(target);

        /// <summary>
        /// Evaluate available transitions and make a weighted random switch.
        /// 
        /// If the new state is the same as the currently active state, nothing
        /// happens.
        /// 
        /// Else active state is exited and the new state is entered
        /// </summary>
        public void CheckTransition(bool avoidActive = false)
        {
            if (InitState()) return;

            if (ActiveState.CheckTransition(personality, avoidActive, out StateType newStateType))
            {
                ForceState(newStateType);
            }
        }

        /// <summary>
        /// Make manager adopt new state if it exists
        /// 
        /// If the new state is the same as the currently active state, nothing
        /// happens.
        /// 
        /// Else active state is exited and the new state is entered
        /// </summary>
        public void ForceState(StateType newStateType)
        {
            var newState = States.FirstOrDefault(s => s.State == newStateType);

            if (newState == null)
            {
                Debug.LogError(PrefixLogMessage($"{ActiveState} wanted to transition to {newStateType}, but we don't know it"));
                return;
            }

            if (ActiveState == newState) return;

            //Taxing personality is done by 
            ActiveState?.Exit();
            ActiveState = newState;
            ActiveState.Enter();
        }

        /// <summary>
        /// Adopts initial state if exists
        /// </summary>
        public void ForceEntryState() =>
            ForceState(EntryState);

        public void UpdateAndForceEntryState(StateType entryState)
        {
            EntryState = entryState;
            ForceEntryState();
        }

        private void OnValidate()
        {
            if (States.Count > 1 && States.SkipLast(1).Contains(States.Last()))
            {
                States[States.Count - 1] = null;
            }

            var problems = States.GroupBy(s => s == null ? StateType.None : s.State).Where(g => g.Count() > 1);
            foreach (var group in problems)
            {
                Debug.LogError(PrefixLogMessage($"There are {group.Count()} alternatives for {group.First()?.State ?? StateType.None} state, this is not allowed. Removing all but first one"));
            }

            foreach (var problem in problems.SelectMany(g => g.Skip(1)))
            {
                States.Remove(problem);
            }
        }
    }
}
