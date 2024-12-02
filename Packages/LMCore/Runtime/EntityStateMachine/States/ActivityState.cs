using LMCore.EntitySM.Trait;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.EntitySM.State
{
    public delegate void EnterStateEvent(ActivityManager manager, ActivityState state);
    public delegate void ExitStateEvent(ActivityManager manager, ActivityState state);
    public delegate void StayStateEvent(ActivityManager manager, ActivityState state);

    public class ActivityState : MonoBehaviour
    {
        [System.Serializable]
        protected struct TraitTax
        {
            [Range(-1f, 1f)]
            public float Agressivity;
            [Range(-1f, 1f)]
            public float Activity;
            [Range(-1f, 1f)]
            public float Explorativity;
            [Range(-1f, 1f)]
            public float Sociability;
            
            public IEnumerable<KeyValuePair<TraitType, float>> Taxes
            {
                get
                {
                    if (Agressivity != 0f) 
                        yield return new KeyValuePair<TraitType, float>(TraitType.Agressivity, Agressivity);
                    if (Activity != 0f) 
                        yield return new KeyValuePair<TraitType, float>(TraitType.Activity, Activity);
                    if (Explorativity != 0f) 
                        yield return new KeyValuePair<TraitType, float>(TraitType.Explorativity, Explorativity);
                    if (Sociability != 0f) 
                        yield return new KeyValuePair<TraitType, float>(TraitType.Sociability, Sociability);
                }
            }
        }

        public static event EnterStateEvent OnEnterState;
        public static event ExitStateEvent OnExitState;
        public static event StayStateEvent OnStayState;

        private ActivityManager _manager;
        public ActivityManager Manager
        {
            get
            {
                if (_manager == null)
                {
                    _manager = GetComponentInParent<ActivityManager>();
                }
                return _manager;
            }
        }

        [SerializeField]
        private StateType _state;
        public StateType State => _state;

        float entryTime;
        bool isAcitveState;
        public float ActiveDuration =>
            isAcitveState ? Time.timeSinceLevelLoad - entryTime : 0f;

        public void Load(bool isActiveSate, float timeSinceEntry)
        {
            this.isAcitveState = isActiveSate;
            entryTime = Time.timeSinceLevelLoad - timeSinceEntry;
        }

        public void Enter()
        {
            entryTime = Time.timeSinceLevelLoad;
            isAcitveState = true;
            OnEnterState?.Invoke(Manager, this);
        }

        public void Exit()
        {
            isAcitveState = false;
            OnExitState?.Invoke(Manager, this);
        }

        private void Update()
        {
            if (isAcitveState)
            {
                OnStayState?.Invoke(Manager, this);
            }
        }

        [SerializeField]
        TraitTax EntryTax;
        /// <summary>
        /// Adjust personality state with regards to entering the state 
        /// </summary>
        public void TaxEnterPersonality(Personality personality)
        {
            foreach (var (trait, tax) in EntryTax.Taxes)
            {
                personality.AdjustState(trait, tax);
            }
        }

        [SerializeField]
        TraitTax ExitTax;
        /// <summary>
        /// Adjust personality state with regards to exiting the state 
        /// </summary>
        public void TaxExitPersonality(Personality personality)
        {
            foreach (var (trait, tax) in ExitTax.Taxes)
            {
                personality.AdjustState(trait, tax);
            }
        }

        [SerializeField]
        TraitTax StayTax;
        /// <summary>
        /// Adjust personality state with regards to staying with the state 
        /// </summary>
        public void TaxStayPersonality(Personality personality)
        {
            foreach (var (trait, tax) in StayTax.Taxes)
            {
                personality.AdjustState(trait, tax);
            }
        }

        [SerializeField]
        List<Transition> Transitions = new List<Transition>();

        /// <summary>
        /// Check if any of the registered edges of the state will trigger
        /// a transition to another state
        /// </summary>
        public bool CheckTransition(Personality personality, out StateType newStateType)
        {
            float totalWeigth = 0;
            var availableTransitions = Transitions
                .Select(t => {
                    var permissable = t.Permissable(personality, out float weight);
                    if (permissable)
                    {
                        totalWeigth += weight;
                    }

                    return new KeyValuePair<Transition, float>(permissable ? t :null, weight);
                    })
                .Where(kvp => kvp.Key != null)
                .ToList();

            newStateType = State;

            if (availableTransitions.Count == 0)
            {
                return false;
            }

            var rng = Random.value * totalWeigth;

            for (int i = 0, n = availableTransitions.Count; i < n; i++)
            {
                var (transition, weight) = availableTransitions[i];
                if (rng <= weight)
                {
                    newStateType = transition.Target;
                    break;
                }
                rng -= weight;
            }

            return true;
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(string.Join("\n", Transitions.Select(t => t.ToString(Manager.Personality))));
        }
    }
}
