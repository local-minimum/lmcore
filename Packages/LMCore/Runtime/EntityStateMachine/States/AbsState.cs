using LMCore.EntitySM.Trait;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.EntitySM.State
{
    public delegate void EnterStateEvent(ActivityManager manager, AbsState state);
    public delegate void ExitStateEvent(ActivityManager manager, AbsState state);
    public delegate void StayStateEvent(ActivityManager manager, AbsState state);

    public abstract class AbsState : MonoBehaviour
    {
        [Serializable]
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

        public abstract StateType State { get; }

        float entryTime;
        bool isAcitveState;
        public float ActiveDuration =>
            isAcitveState ? Time.timeSinceLevelLoad - entryTime : 0f;

        protected abstract void _Enter();
        public void Enter()
        {
            entryTime = Time.timeSinceLevelLoad;
            isAcitveState = true;
            _Enter();
            OnEnterState?.Invoke(Manager, this);
        }

        protected abstract void _Exit();
        public void Exit()
        {
            isAcitveState = false;
            _Exit();
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

        /// <summary>
        /// Check if any of the registered edges of the state will trigger
        /// a transition to another state
        /// </summary>
        public abstract bool CheckTransition(Personality personality, out StateType newStateType);
    }
}
