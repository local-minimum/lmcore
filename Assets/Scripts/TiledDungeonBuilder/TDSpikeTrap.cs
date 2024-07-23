using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiledDungeon
{
    public class TDSpikeTrap : MonoBehaviour
    {
        private enum SpikePhase { Retracted, Extending, Extended, Retracting };

        [SerializeField]
        GameObject[] Spikes;

        [SerializeField]
        bool Spikeless = false;

        [SerializeField]
        float retractedTime = 2f;

        [SerializeField]
        float extendedTime = 0.4f;

        SpikePhase phase = SpikePhase.Retracted;

        [SerializeField]
        Animator animator;

        float nextPhase;


        void Start()
        {
            if (Spikeless)
            {
                foreach (var spike in Spikes)
                {
                    spike.SetActive(false);
                }
            }

            nextPhase = Time.timeSinceLevelLoad + retractedTime;
        }

        bool blockingEntry;

        public bool BlockingEntry
        {
            get
            {
                return blockingEntry || phase == SpikePhase.Extending;
            }
        }

        public void HandleAnimationDone()
        {
            if (phase == SpikePhase.Extending)
            {
                nextPhase = Time.timeSinceLevelLoad + extendedTime;
                phase = SpikePhase.Extended;
            } else if (phase == SpikePhase.Retracting)
            {
                nextPhase = Time.timeSinceLevelLoad + retractedTime;
                phase = SpikePhase.Retracted;
            } else
            {
                phase = SpikePhase.Retracted;
            }
        }

        public void HandleBlockEntry()
        {
            blockingEntry = true;
        }

        public void AllowEntry()
        {
            blockingEntry = false;
        }

        public void HurtEntity()
        {

        }

        public void PushEntity()
        {

        }

        private void Update()
        {
            if (Time.timeSinceLevelLoad > nextPhase)
            {
                switch (phase)
                {
                    case SpikePhase.Retracted:
                        phase = SpikePhase.Extending;
                        animator.SetTrigger("Extend");
                        break;
                    case SpikePhase.Extended:
                        phase = SpikePhase.Retracting;
                        animator.SetTrigger("Retract");
                        break;
                }
            }
        }
    }
}
