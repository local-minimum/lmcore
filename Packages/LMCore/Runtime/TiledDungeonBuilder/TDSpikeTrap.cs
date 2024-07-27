using System.Collections.Generic;
using System.Linq;
using LMCore.Crawler;
using LMCore.TiledImporter;
using LMCore.TiledDungeon.Integration;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDSpikeTrap : MonoBehaviour
    {
        enum Management { Automatic, ToggleGroup, Sequencer };

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

        TiledCustomProperties SequenceGroupProps => node
            .FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.ObjSequencerGroupClass);

        [SerializeField, HideInInspector]
        Management management;

        int sequenceId;
        int sequenceNextPhaseId;

        bool startSequence;

        bool managed => management != Management.Automatic;

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
            Spikeless = node.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.WallSpikeTrapClass)
                ?.Bool(TiledConfiguration.instance.ObjSpikelessKey) ?? false;
            
            anchor = modifications.FirstOrDefault(mod =>
                mod.Tile.Type == TiledConfiguration.instance.WallSpikeTrapClass)?.Tile
                    .CustomProperties
                    .Direction(TiledConfiguration.instance.AnchorKey).AsDirection() ?? Direction.None;

            if (anchor == Direction.None)
            {
                Debug.LogError($"Spikes @ {position} lacks anchor direction");
            }

            // TODO: Improve this logging
            Debug.Log($"Spike Trap @ {position}: Spikeless({Spikeless}) Anchor({anchor})");

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
            var sequenceGroup = SequenceGroupProps;

            if (toggleGroups.Count > 0)
            {
                management = Management.ToggleGroup;
                foreach (var toggleGroup in toggleGroups)
                {
                    ToggleGroup.instance.RegisterReciever(toggleGroup, Extend);
                }
            } else if (sequenceGroup != null)
            {
                management = Management.Sequencer;
                sequenceId = sequenceGroup.Int(TiledConfiguration.instance.ObjGroupKey);
                var phaseId = sequenceGroup.Int(TiledConfiguration.instance.ObjPhaseKey);

                Sequencer.instance.RegisterReciever(sequenceId, phaseId, Ready);
                startSequence = sequenceGroup.Bool(TiledConfiguration.instance.ObjSequenceStarter);

                sequenceNextPhaseId = sequenceGroup.Int(TiledConfiguration.instance.ObjNextPhaseKey, -1);
                retractedTime = sequenceGroup.Float(TiledConfiguration.instance.ObjDelayTimeKey, retractedTime);
            } else
            {
                management = Management.Automatic;
            }

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
                Ready();
            } else
            {
                phase = SpikePhase.Waiting;

                if (management == Management.Sequencer && sequenceNextPhaseId >= 0)
                {
                    Sequencer.instance.Invoke(sequenceId, sequenceNextPhaseId);
                }
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

        void Ready()
        {
            phase = SpikePhase.Retracted;
            SetNextPhaseTime();
            Debug.Log($"Spikes @ {position} are now ready and will extend");
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
            if (management == Management.Sequencer && startSequence)
            {
                Sequencer.instance.Invoke(sequenceId, sequenceNextPhaseId);
                startSequence = false;
            }

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
