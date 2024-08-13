using LMCore.Extensions;
using UnityEngine;

namespace LMCore.Juice
{
    public class Transition : MonoBehaviour
    {
        public delegate void PhaseChangeEvent(Phase phase);

        public event PhaseChangeEvent OnPhaseChange;

        [SerializeField]
        int animState = 0;

        [SerializeField]
        Animator anim;

        [SerializeField]
        string InitiateTrigger = "Initiate";

        [SerializeField]
        string FinalizeTrigger = "Finalize";

        [SerializeField]
        string WaitingTrigger = "Waiting";

        [SerializeField]
        GameObject EffectRoot;

        protected string PrefixLogMessage(string message) => $"Transition {name}: {message}";

        public enum Phase { Inacive, EaseIn, Waiting, EaseOut, Completed };

        private Phase phase = Phase.Inacive;
        public Phase ActivePhase
        {
            get { return phase; }
            set { 
            
                if (phase == value) return;

                if (value == Phase.Inacive || !anim.enabled)
                {
                    anim.enabled = true;
                    anim.PlayInFixedTime(animState);
                }

                if ((value == Phase.Inacive || value == Phase.Completed) == EffectRoot.activeSelf)
                {
                    EffectRoot.SetActive(!EffectRoot.activeSelf);
                }

                switch (value)
                {

                    case Phase.EaseIn:
                        anim?.SetTrigger(InitiateTrigger);
                        break;
                    case Phase.EaseOut:
                        anim?.SetTrigger(FinalizeTrigger);
                        break;
                    case Phase.Waiting:
                        anim?.SetTrigger(WaitingTrigger);
                        break;
                }

                phase = value; 
                OnPhaseChange?.Invoke(phase);
            }
        }

        void Awake()
        {
            StopAnimation(Phase.Inacive);
        }

        [SerializeField]
        AnimationClip waitingClip;

        [SerializeField]
        AnimationClip fadeOutClip;

        private void Update()
        {
            if (!anim.enabled) return;

            if (ActivePhase != Phase.Waiting && anim.IsAnimating(animState, waitingClip.name))
            {
                Debug.Log(PrefixLogMessage("Currently waiting"));
                phase = Phase.Waiting;
                OnPhaseChange?.Invoke(phase);
            }

            if (ActivePhase != Phase.Inacive && anim.IsActiveAnimation(animState, fadeOutClip.name) && !anim.IsAnimating(animState, fadeOutClip.name))
            {
                StopAnimation(Phase.Completed);
            }
        }

        private void StopAnimation(Phase phase)
        {
            Debug.Log(PrefixLogMessage(phase.ToString()));
            this.phase = phase;
            EffectRoot.SetActive(false);
            anim.StopPlayback();
            anim.enabled = false;

            OnPhaseChange?.Invoke(phase);
        }


        [ContextMenu("Start Cross-fade")]
        private void StartAnimation()
        {
            ActivePhase = Phase.EaseIn;
        }

        [ContextMenu("Complete Cross-fade")]
        private void CompleteAnimation()
        {
            ActivePhase = Phase.EaseOut;
        }
    }
}
