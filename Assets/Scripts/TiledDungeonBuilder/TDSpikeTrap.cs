using LMCore.Crawler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TiledDungeon.Integration;
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

        [SerializeField, HideInInspector]
        Direction anchor;

        [SerializeField, HideInInspector]
        Vector3Int position;

        [SerializeField]
        string ExtendTrigger = "Extend";

        [SerializeField]
        string RetractTrigger = "Retract";

        TDNode _node;
        TDNode node
        {
            get
            {
                if (_node == null)
                {
                    _node = GetComponentInParent<TDNode>();
                }
                return _node;
            }
        }

        void Start()
        {
            Synch();
            nextPhase = Time.timeSinceLevelLoad + retractedTime;
        }

        public void Configure(
            TDNode node,
            Vector3Int position,
            TileModification[] modifications
        )
        {
            this.position = position;
            Spikeless = node.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.SpikeTrapClass)
                ?.Bool(TiledConfiguration.instance.ObjSpikelessKey) ?? false;
            
            anchor = modifications.FirstOrDefault(mod =>
                mod.Tile.Type == TiledConfiguration.instance.SpikeTrapClass)?.Tile
                    .CustomProperties
                    .Direction(TiledConfiguration.instance.AnchorKey).AsDirection() ?? Direction.None;

            if (anchor == Direction.None)
            {
                Debug.LogError($"Spikes @ {position} lacks anchor direction");
            }

            Synch();
        }

        void Synch()
        {
            if (Spikeless)
            {
                foreach (var spike in Spikes)
                {
                    spike.SetActive(false);
                }
            }
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
            if (Spikeless) return;

            foreach (var occupant in node.Occupants)
            {
                // TODO: Hurt them!
            }
        }

        public void PushEntity()
        {
            var push = anchor.Inverse();

            foreach (var occupant in node.Occupants)
            {
                var movement = push.AsMovement(occupant.LookDirection, occupant.Anchor);
                Debug.Log($"Spikes @ {position}: Push {occupant.name} {push} using {movement}");
                occupant.Input.InjectMovement(movement);
            }
        }

        private void Update()
        {
            if (Time.timeSinceLevelLoad > nextPhase)
            {
                switch (phase)
                {
                    case SpikePhase.Retracted:
                        phase = SpikePhase.Extending;
                        animator.SetTrigger(ExtendTrigger);
                        break;
                    case SpikePhase.Extended:
                        phase = SpikePhase.Retracting;
                        animator.SetTrigger(RetractTrigger);
                        break;
                }
            }
        }
    }
}
