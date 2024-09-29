using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Inventory;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.Actions;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.IO;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    // TODO: Use configuration to present lock and button
    public class TDDoor : MonoBehaviour, IOnLoadSave
    {
        private enum Transition { None, Opening, Closing };

        [SerializeField, HideInInspector]
        bool isOpen = false;

        [SerializeField, HideInInspector]
        Vector3Int _Position;
        public Vector3Int Coordinates => _Position;

        [SerializeField, HideInInspector]
        TileModification[] modifications;

        [SerializeField, HideInInspector]
        TDNode node;

        [SerializeField]
        AbstractDungeonAction[] OpenActions;

        [SerializeField]
        AbstractDungeonAction[] CloseActions;

        bool isLocked;

        string key;

        bool consumesKey;

        bool automaticTrapDoor;

        [SerializeField]
        float autoCloseTime = 0.5f;

        public override string ToString() =>
            $"Door Axis({TraversalAxis}) Blocking({BlockingPassage}) Transition({ActiveTransition})";

        protected string PrefixLogMessage(string message) => $"Door @ {_Position}: {message}";

        [ContextMenu("Info")]
        void Info() => Debug.Log(this);

        Transition ActiveTransition
        {
            get
            {
                for (int i = 0; i < OpenActions.Length; i++)
                {
                    if (OpenActions[i].IsEasing) return Transition.Opening;
                }
                for (int i = 0; i < CloseActions.Length; i++)
                {
                    if (CloseActions[i].IsEasing) return Transition.Closing;
                }

                return Transition.None;
            }
        }

        public bool BlockingPassage
        {
            get
            {
                if (ActiveTransition == Transition.Closing)
                {
                    Debug.Log(PrefixLogMessage("closing"));
                    return true;
                } else if (TraversalAxis == DirectionAxis.UpDown && ActiveTransition == Transition.Opening)
                {
                    // We are a trapdoor we need to let player fall through
                    return false;
                }

                return !isOpen;
            }
        }

        public bool FullyClosed => !isOpen && ActiveTransition == Transition.None;

        public DirectionAxis TraversalAxis
        {
            get
            {
                return modifications
                    .First()
                    .Tile
                    .CustomProperties
                    .Orientation(TiledConfiguration.instance.TraversalAxisKey)
                    .AsAxis();
            }
        }

        public int OnLoadPriority => 500;

        bool synced = false;

        private void Start()
        {
            if (node != null && !synced)
            {
                SyncDoor();
            }
        }

        public void Configure(
            TDNode node,
            Vector3Int position, 
            TileModification[] modifications
        )
        {
            _Position = position;
            this.modifications = modifications;
            this.node = node;

            SyncDoor();
        }

        private void OnEnable()
        {
            GridEntity.OnInteract += GridEntity_OnInteract;
            GridEntity.OnMove += GridEntity_OnMove;
            TDNode.OnNewOccupant += TDNode_OnNewOccupant;
        }

        private void OnDisable()
        {
            GridEntity.OnInteract -= GridEntity_OnInteract;
            GridEntity.OnMove -= GridEntity_OnMove;
            TDNode.OnNewOccupant -= TDNode_OnNewOccupant;
        }

        private void GridEntity_OnMove(GridEntity entity)
        {
            if (entity.Moving != MovementType.Stationary)
            {
                activelyMovingEntities.Add(entity);
            } else
            {
                activelyMovingEntities.Remove(entity);
            }
        }

        HashSet<GridEntity> trapTriggeringEntities = new HashSet<GridEntity>();

        private void TDNode_OnNewOccupant(TDNode node, GridEntity entity)
        {
            if (entity.Coordinates != Coordinates)
            {
                if (trapTriggeringEntities.Contains(entity))
                {
                    trapTriggeringEntities.Remove(entity);
                    
                    if (trapTriggeringEntities.Count == 0 && (isOpen || ActiveTransition == Transition.Opening))
                    {
                        StartCoroutine(AutoClose(PrefixLogMessage($"automatically closes after {entity.name}")));
                    }
                }

                return;
            }

            if (entity.TransportationMode.HasFlag(TransportationMode.Flying)
                || entity.AnchorDirection != Direction.Down) return;

            if (automaticTrapDoor && ActiveTransition != Transition.Opening && !isOpen)
            {
                trapTriggeringEntities.Add(entity);
                OpenDoor();
                entity.Falling = true; 
                Debug.Log(PrefixLogMessage($"automatically opens for {entity.name}"));
            }
        }


        bool AutomaticTrapdoorAction(GridEntity entity)
        {
            return automaticTrapDoor
                && entity.Coordinates == Coordinates
                && entity.AnchorDirection == Direction.Down
                && !entity.TransportationMode.HasFlag(TransportationMode.Climbing)
                && !entity.TransportationMode.HasFlag(TransportationMode.Flying);
        }

        IEnumerator<WaitForSeconds> AutoClose(string logMessage)
        {
            yield return new WaitForSeconds(autoCloseTime);
            if (isOpen || ActiveTransition == Transition.Opening)
            {
                CloseDoor();
                if (!string.IsNullOrEmpty(logMessage)) Debug.Log(logMessage);
            }
        }

        HashSet<GridEntity> activelyMovingEntities = new();

        private void GridEntity_OnInteract(GridEntity entity)
        {
            var onTheMove = activelyMovingEntities.Contains(entity);
            var validPosition = entity.LookDirection.Translate(entity.Coordinates) == _Position;

            if (!onTheMove && validPosition)
            {
                Debug.Log(PrefixLogMessage("Attempting to open door"));

                if (isLocked)
                {
                    var keyHolder = entity
                        .GetComponentsInChildren<AbsInventory>()
                        .FirstOrDefault(i => i.HasItem(key));

                    if (keyHolder == null)
                    {
                        Debug.LogWarning(PrefixLogMessage($"requires key ({key})"));
                        return;
                    }

                    if (consumesKey && !keyHolder.Consume(key, out string _))
                    {
                        Debug.LogWarning(PrefixLogMessage($"Failed to consume key {key} from {keyHolder}"));
                    }
                    isLocked = false;
                }

                Interact();
            }
        }

        void MapOverActions(
            AbstractDungeonAction[] actions,
            System.Action<AbstractDungeonAction> action
        )
        {
            for (int i=0; i<actions.Length; i++)
            {
                action(actions[i]);
            }
        }

        void CloseDoor()
        {
            var transition = ActiveTransition;
            if (transition == Transition.Closing) return;

            if (transition == Transition.Opening)
            {
                MapOverActions(OpenActions, (action) => action.Abandon());
                MapOverActions(CloseActions, (action) => action.PlayFromCurrentProgress(() => isOpen = false));
            } else
            {
                MapOverActions(CloseActions, (action) => action.Play(() => isOpen = false));
            }
        }

        void OpenDoor()
        {
            var transition = ActiveTransition;
            if (transition == Transition.Opening) return;

            if (transition == Transition.Closing)
            {
                MapOverActions(CloseActions, (action) => action.Abandon());
                MapOverActions(OpenActions, (action) => action.PlayFromCurrentProgress(() => isOpen = true));
            } else
            {
                MapOverActions(OpenActions, (action) => action.Play(() => isOpen = true));
            }
        }

        [ContextMenu("Interact")]
        public void Interact()
        {
            Debug.Log(PrefixLogMessage($"Toggling door from Open({isOpen} / {ActiveTransition})"));
            switch (ActiveTransition)
            {
                case Transition.None:
                    if (isOpen)
                    {
                        CloseDoor();
                    } else
                    {
                        OpenDoor();
                    }
                    break;
                case Transition.Opening:
                    CloseDoor();
                    break;
                case Transition.Closing:
                    OpenDoor();
                    break;
                default:
                    Debug.LogError(PrefixLogMessage($"Unhandled transition: {ActiveTransition}"));
                    break;
            }
        }

        void SyncDoor()
        {
            var config = node.Config;

            var toggleGroups = config 
                .GetObjectValues(
                    TiledConfiguration.instance.ObjToggleGroupClass,
                    props => props.Int(TiledConfiguration.instance.ObjGroupKey)
                )
                .Where(group => group > 0)
                .ToHashSet();

            var toggleGroup = GetComponentInParent<ToggleGroup>();
            foreach (var group in toggleGroups)
            {
                toggleGroup.RegisterReciever(group, Interact);
            }

            automaticTrapDoor = config.GetObjectValues(
                TiledConfiguration.instance.TrapDoorClass,
                props => props.Interaction(TiledConfiguration.instance.InteractionKey) == TDEnumInteraction.Automatic
            ).Any();

            isOpen = config.FirstObjectValue(
                TiledConfiguration.instance.ObjInitialClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.OpenKey)
            );

            MapOverActions(isOpen ? OpenActions : CloseActions, (action) => {
                action.Play(null);
                action.Finalise();
            });

            isLocked = modifications.Any(
                mod => mod.Tile.CustomProperties.InteractionOrDefault(
                    TiledConfiguration.instance.InteractionKey,
                    TDEnumInteraction.Open) == TDEnumInteraction.Locked
            );

            key = config.FirstObjectValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props?.String(TiledConfiguration.instance.KeyKey)
            );
            consumesKey = config.FirstObjectValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.ConusumesKeyKey)
            );

            Debug.Log(PrefixLogMessage(
                $"Synced as Locked({isLocked}) Key({key}; consumes={consumesKey}) Open({isOpen}) Automatic({automaticTrapDoor})"
            ));
            synced = true;
        }

        public void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }
            if (!synced) SyncDoor();

            var lvl = GetComponentInParent<IDungeon>().MapName;

            var doorSave = save.levels[lvl]?.doors?.GetValueOrDefault(_Position);

            if (doorSave == null)
            {
                Debug.LogError(PrefixLogMessage("I have no saved state"));
                return;
            }

            isOpen = doorSave.isOpen;
            isLocked = doorSave.isLocked;

            MapOverActions(isOpen ? OpenActions : CloseActions, (action) => {
                action.Play(null);
                action.Finalise();
            });

            Debug.Log(PrefixLogMessage($"Loaded as isOpen({isOpen}) and isLocked({isLocked})"));
        }

        public DoorSave Save()
        {
            return new DoorSave(isOpen, isLocked);
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }

        private void Update()
        {
            if (activelyMovingEntities.Any(AutomaticTrapdoorAction))
            {
                if (ActiveTransition != Transition.Opening && !isOpen)
                {
                    OpenDoor();
                }
            }

        }
    }
}
