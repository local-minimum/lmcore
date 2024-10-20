using System.Collections.Generic;
using System.Linq;
using LMCore.Crawler;
using LMCore.TiledImporter;
using LMCore.TiledDungeon.Integration;
using UnityEngine;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDSpikeTrap : TDFeature, IOnLoadSave
    {
        enum Management { Automatic, ToggleGroup, Sequencer };

        [SerializeField]
        public enum SpikePhase { Retracted, Extending, Extended, Retracting, Waiting };

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

        [SerializeField]
        string ExtendTrigger = "Extend";

        [SerializeField]
        string RetractTrigger = "Retract";

        [SerializeField]
        string RetractedTrigger = "Retracted";

        [SerializeField]
        string ExtendedTrigger = "Extended";

        protected string PrefixLogMessage(string message) => $"Spikes @ {Coordinates}: {message}";

        HashSet<int> ToggleGroups => Node
            .Config
            .GetObjectValues(
                TiledConfiguration.instance.ObjToggleGroupClass,
                props => props.Int(TiledConfiguration.instance.ObjGroupKey)
            )
            .Where(group => group > 0)
            .ToHashSet();

        TiledCustomProperties SequenceGroupProps => Node
            .Config
            .FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.ObjSequencerGroupClass);

        [SerializeField, HideInInspector]
        Management management;

        int sequenceId;
        int sequenceNextPhaseId;

        bool startSequence;

        bool managed => management != Management.Automatic;

        void Start()
        {
            InitStartCoordinates();
            Sync();
            SetNextPhaseTime();
        }

        public void Configure(
            TDNode node,
            TileModification[] modifications
        )
        {
            Spikeless = node.Config.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.WallSpikeTrapClass)
                ?.Bool(TiledConfiguration.instance.ObjSpikelessKey) ?? false;
            
            anchor = modifications.FirstOrDefault(mod =>
                mod.Tile.Type == TiledConfiguration.instance.WallSpikeTrapClass)?.Tile
                    .CustomProperties
                    .Direction(TiledConfiguration.instance.AnchorKey).AsDirection() ?? Direction.None;

            if (anchor == Direction.None)
            {
                Debug.Log(PrefixLogMessage("lacks anchor direction, assuming down"));
                anchor = Direction.Down;
            }

            Debug.Log(PrefixLogMessage($"Spikeless({Spikeless}) Anchor({anchor})"));

            Sync();
        }

        ToggleGroup _ToggleGroup;
        ToggleGroup ToggleGroup
        {
            get
            {
                if (_ToggleGroup == null)
                {
                    _ToggleGroup = GetComponentInParent<ToggleGroup>();
                }
                return _ToggleGroup;
            }
        }

        Sequencer _Sequencer;
        Sequencer Sequencer
        {
            get
            {
                if (_Sequencer == null)
                {
                    _Sequencer = GetComponentInParent<Sequencer>();
                }
                return _Sequencer;
            }
        }

        bool synced = false;

        void Sync()
        {
            if (synced) return;

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
                    ToggleGroup.RegisterReciever(toggleGroup, Extend);
                }
            } else if (sequenceGroup != null)
            {
                management = Management.Sequencer;
                sequenceId = sequenceGroup.Int(TiledConfiguration.instance.ObjGroupKey);
                var phaseId = sequenceGroup.Int(TiledConfiguration.instance.ObjPhaseKey);

                Sequencer.RegisterReciever(sequenceId, phaseId, Ready);
                startSequence = sequenceGroup.Bool(TiledConfiguration.instance.ObjSequenceStarter);

                sequenceNextPhaseId = sequenceGroup.Int(TiledConfiguration.instance.ObjNextPhaseKey, -1);
                retractedTime = sequenceGroup.Float(TiledConfiguration.instance.ObjDelayTimeKey, retractedTime);
            } else
            {
                management = Management.Automatic;
            }

            phase = managed ? SpikePhase.Waiting : SpikePhase.Retracted;
            synced = true;
        }

        bool blockingEntry;

        public bool BlockingEntry
        {
            get
            {
                return blockingEntry || phase == SpikePhase.Extending;
            }
        }

        public int OnLoadPriority => 500;

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
                    Sequencer.Invoke(sequenceId, sequenceNextPhaseId);
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

            foreach (var occupant in Node.Occupants)
            {
                // TODO: Hurt them!
            }
        }

        public void PushEntity()
        {
            var push = anchor.Inverse();

            foreach (var occupant in Node.Occupants)
            {
                var movement = push.AsMovement();
                Debug.Log(PrefixLogMessage($"Push {occupant.name} {push} using {movement}"));
                occupant.Input.InjectMovement(movement);
            }
        }

        void Ready()
        {
            phase = SpikePhase.Retracted;
            SetNextPhaseTime();
            Debug.Log(PrefixLogMessage("now ready and will extend"));
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
                Sequencer.Invoke(sequenceId, sequenceNextPhaseId);
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

        public void OnLoadGameSave(GameSave save)
        {
            Sync();

            if (save == null)
            {
                return;
            }

            var lvl = GetComponentInParent<IDungeon>().MapName;

            var trapSave = save.levels[lvl]?.spikes?.GetValueOrDefault(StartCoordinates);

            if (trapSave == null)
            {
                Debug.LogError(PrefixLogMessage("No save state exists for me"));
                return;
            }

            phase = trapSave.phase;

            switch (phase) {
                case SpikePhase.Retracted:
                    animator.SetTrigger(RetractedTrigger);
                    SetNextPhaseTime();
                    break;
                case SpikePhase.Extended:
                    animator.SetTrigger(ExtendedTrigger);
                    SetNextPhaseTime();
                    break;
                case SpikePhase.Retracting:
                    Retract();
                    break;
                case SpikePhase.Extending:
                    Extend();
                    break;
                case SpikePhase.Waiting:
                    // TODO: Check that sequenced cant get stuck loading...
                    break;
            }
        }

        public KeyValuePair<Vector3Int, SpikeTrapSave> Save() => 
            new KeyValuePair<Vector3Int, SpikeTrapSave>(
                StartCoordinates,
                new SpikeTrapSave(phase));

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
    }
}
