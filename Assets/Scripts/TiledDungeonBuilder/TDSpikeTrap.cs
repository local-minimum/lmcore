using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using TiledDungeon.Integration;
using UnityEngine;

namespace TiledDungeon
{
    public class TDSpikeTrap : MonoBehaviour
    {
        private enum SpikePhase { Retracted, Extending, Extended, Retracting, Waiting };

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

        float nextPhaseTime;

        [SerializeField, HideInInspector]
        Direction anchor;

        [SerializeField, HideInInspector]
        Vector3Int position;

        [SerializeField]
        string ExtendTrigger = "Extend";

        [SerializeField]
        string RetractTrigger = "Retract";

        HashSet<int> ToggleGroups => node
            .GetObjectValues(
                TiledConfiguration.instance.ObjToggleGroupClass,
                props => props.Int(TiledConfiguration.instance.ObjGroupKey)
            )
            .Where(group => group > 0)
            .ToHashSet();

        bool managed;

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
            SetNextPhaseTime();
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

            Debug.Log($"Spike Trap @ {position}: Spikeless({Spikeless}) Anchor({anchor}) Groups([{string.Join(", ", ToggleGroups)}])");

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

            var toggleGroups = ToggleGroups;
            foreach (var toggleGroup in toggleGroups)
            {
                ToggleGroup.instance.RegisterReciever(toggleGroup, Extend);
            }

            managed = toggleGroups.Count > 0;

            phase = managed ? SpikePhase.Waiting : SpikePhase.Retracted;
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
                phase = SpikePhase.Extended;
            } else if (phase == SpikePhase.Retracting && !managed)
            {
                phase = SpikePhase.Retracted;
            } else
            {
                phase = SpikePhase.Waiting;
            }
            SetNextPhaseTime();
        }

        void SetNextPhaseTime()
        {

            switch (phase)
            {
                case SpikePhase.Retracted:
                    nextPhaseTime = Time.timeSinceLevelLoad + retractedTime;
                    break;
                case SpikePhase.Extended:
                    nextPhaseTime = Time.timeSinceLevelLoad + extendedTime;
                    break;
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
                var movement = push.AsMovement();
                Debug.Log($"Spikes @ {position}: Push {occupant.name} {push} using {movement}");
                occupant.Input.InjectMovement(movement);
            }
        }

        void Extend()
        {
            phase = SpikePhase.Extending;
            animator.SetTrigger(ExtendTrigger);
        }

        void Retract()
        {
            phase = SpikePhase.Retracting;
            animator.SetTrigger(RetractTrigger);
        }
        private void Update()
        {
            if (Time.timeSinceLevelLoad > nextPhaseTime)
            {
                switch (phase)
                {
                    case SpikePhase.Retracted:
                        Extend();
                        break;
                    case SpikePhase.Extended:
                        Retract();
                        break;
                }
            }
        }
    }
}
