using LMCore.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LMCore.Juice
{
    public class Transition : MonoBehaviour
    {
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

        public enum Phase { Inacive, EaseIn, Waiting, EaseOut };

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

                if ((value != Phase.Inacive) != EffectRoot.activeSelf)
                {
                    EffectRoot.SetActive(value != Phase.Inacive);
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
            }
        }

        void Awake()
        {
            StopAnimation();
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
            }

            if (ActivePhase != Phase.Inacive && anim.IsActiveAnimation(animState, fadeOutClip.name) && !anim.IsAnimating(animState, fadeOutClip.name))
            {
                StopAnimation(true);
            }
        }

        private void StopAnimation(bool unload = false)
        {
            Debug.Log(PrefixLogMessage("Shutting down"));
            phase = Phase.Inacive;
            EffectRoot.SetActive(false);
            anim.StopPlayback();
            anim.enabled = false;

            if (unload)
            {
                SceneManager.UnloadSceneAsync(gameObject.scene.name);
            }
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
