using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Inventory;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.Actions;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.IO;
using LMCore.UI;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDDoor : TDFeature, IOnLoadSave
    {
        private enum Transition { None, Opening, Closing };

        [SerializeField, HideInInspector]
        bool isOpen = false;

        [SerializeField, HideInInspector]
        TileModification[] modifications;

        [SerializeField]
        AbstractDungeonAction[] OpenActions;

        [SerializeField]
        AbstractDungeonAction[] CloseActions;

        bool isLocked;

        string key;

        bool consumesKey;

        bool automaticTrapDoor;
        bool isTrapdoor;

        [SerializeField]
        float autoCloseTime = 0.5f;

        public override string ToString() =>
            $"{(isTrapdoor ? "Trap-" : "")}Door Axis({TraversalAxis}) Blocking({BlockingPassage}) Transition({ActiveTransition})";

        protected string PrefixLogMessage(string message) => $"Door @ {Coordinates}: {message}";

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
                else if (ActiveTransition == Transition.Opening)
                {
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
            if (!synced)
            {
                SyncDoor();
            }
        }

        public void Configure(
            TileModification[] modifications
        )
        {
            this.modifications = modifications;

            SyncDoor();
        }

        private void OnEnable()
        {
            GridEntity.OnInteract += GridEntity_OnInteract;
            GridEntity.OnMove += GridEntity_OnMove;
            GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
            TDNode.OnNewOccupant += TDNode_OnNewOccupant;
        }

        private void OnDisable()
        {
            GridEntity.OnInteract -= GridEntity_OnInteract;
            GridEntity.OnMove -= GridEntity_OnMove;
            GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
            TDNode.OnNewOccupant -= TDNode_OnNewOccupant;
        }

        string lastPrompt;
        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            if (isTrapdoor || entity.EntityType != GridEntityType.PlayerCharacter) return;

            var validPosition = entity.LookDirection.Translate(entity.Coordinates) == Coordinates;
            if (!validPosition)
            {
                HideLastPrompt();
                return;
            }

            ShowPrompt(entity);
        }

        private void ShowPrompt(GridEntity entity)
        { 
            if (isTrapdoor) return;

            if (entity != null)
            {
                var validPosition = entity.LookDirection.Translate(entity.Coordinates) == Coordinates;
                if (!validPosition) return;
            }

            var bindingsUI = MovementKeybindingUI.InstanceOrResource("Keybinding");
            var keyHint = bindingsUI.GetActionHint(GamePlayAction.Interact);

            if (isLocked)
            {
                var keyHolder = entity
                    .GetComponentsInChildren<AbsInventory>()
                    .FirstOrDefault(i => i.HasItem(key));

                if (keyHolder == null)
                {
                    lastPrompt = "Door locked";
                }
                else
                {
                    lastPrompt = $"{keyHint} Unlock door";
                }
            } else if (isOpen)
            {
                lastPrompt = $"{keyHint} Close door";
            } else
            {
                lastPrompt = $"{keyHint} Open door";
            }
            PromptUI.instance.ShowText(lastPrompt);
        }

        private void HideLastPrompt()
        {
            if (!string.IsNullOrEmpty(lastPrompt))
            {
                PromptUI.instance.HideText(lastPrompt);
                lastPrompt = null;
            }
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
                OpenDoor(null);
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
                CloseDoor(null);
                if (!string.IsNullOrEmpty(logMessage)) Debug.Log(logMessage);
            }
        }

        HashSet<GridEntity> activelyMovingEntities = new();

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (isTrapdoor) return;

            var onTheMove = activelyMovingEntities.Contains(entity);
            var validPosition = entity.LookDirection.Translate(entity.Coordinates) == Coordinates;

            if (!onTheMove && validPosition)
            {
                Debug.Log(PrefixLogMessage("Attempting to open door"));

                HideLastPrompt();

                if (isLocked)
                {
                    var keyHolder = entity != null ? entity
                        .GetComponentsInChildren<AbsInventory>()
                        .FirstOrDefault(i => i.HasItem(key)) : null;

                    if (keyHolder == null)
                    {
                        Debug.LogWarning(PrefixLogMessage($"requires key ({key})"));
                        return;
                    }

                    if (consumesKey) { 
                        if (keyHolder.Consume(key, out string _))
                        {
                            PromptUI.instance.ShowText("Lost key", 2);
                        } else
                        {
                            Debug.LogWarning(PrefixLogMessage($"Failed to consume key {key} from {keyHolder}"));
                        }
                    }
                    isLocked = false;
                    isOpen = false;
                }

                Interact(entity);
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

        void CloseDoor(GridEntity entity)
        {
            var transition = ActiveTransition;
            if (transition == Transition.Closing) return;

            if (transition == Transition.Opening)
            {
                MapOverActions(OpenActions, (action) => action.Abandon());
                MapOverActions(CloseActions, (action) => action.PlayFromCurrentProgress(() => {
                    if (isOpen)
                    {
                        isOpen = false;
                        ShowPrompt(entity);
                    }
                }));
            } else
            {
                MapOverActions(CloseActions, (action) => action.Play(() =>
                {
                    if (isOpen)
                    {
                        isOpen = false;
                        ShowPrompt(entity);
                    }
                }));
            }
        }

        [SerializeField]
        float considerOpenAfterProgress = 0.4f;

        void OpenDoor(GridEntity entity)
        {
            var transition = ActiveTransition;
            if (transition == Transition.Opening) return;

            if (transition == Transition.Closing)
            {
                MapOverActions(CloseActions, (action) => action.Abandon());
                MapOverActions(OpenActions, (action) => action.PlayFromCurrentProgress(
                    () =>
                    {
                        if (!isOpen)
                        {
                            isOpen = true;
                            ShowPrompt(entity);
                        }
                    }, 
                    (progress) => {
                        if (!isOpen && progress > considerOpenAfterProgress)
                        {

                            ShowPrompt(entity);
                        }
                    }));
            } else
            {
                MapOverActions(OpenActions, 
                    (action) => action.Play(() =>
                    {
                        if (!isOpen)
                        {
                            isOpen = true;
                            ShowPrompt(entity);
                        }
                    }, 
                    (progress) => {
                        if (!isOpen && progress > considerOpenAfterProgress)
                        {

                            ShowPrompt(entity);
                        }
                    }));
            }
        }

        [ContextMenu("Interact")]
        public void Interact() => Interact(null);
        public void Interact(GridEntity entity)
        {
            Debug.Log(PrefixLogMessage($"Toggling door from Open({isOpen} / {ActiveTransition})"));
            switch (ActiveTransition)
            {
                case Transition.None:
                    if (isOpen)
                    {
                        CloseDoor(entity);
                    } else
                    {
                        OpenDoor(entity);
                    }
                    break;
                case Transition.Opening:
                    CloseDoor(entity);
                    break;
                case Transition.Closing:
                    OpenDoor(entity);
                    break;
                default:
                    Debug.LogError(PrefixLogMessage($"Unhandled transition: {ActiveTransition}"));
                    break;
            }
        }

        void SyncDoor()
        {
            if (synced) return;

            var node = Node;
            if (node == null) return;

            InitStartCoordinates();

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

            isTrapdoor = node.modifications.Any(m => m.Tile.Type == TiledConfiguration.instance.TrapDoorClass);

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

        private void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }
            if (!synced) SyncDoor();

            var lvl = Dungeon.MapName;

            var doorSave = save.levels[lvl]?.doors?.GetValueOrDefault(StartCoordinates);

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

        public KeyValuePair<Vector3Int, DoorSave> Save() => new KeyValuePair<Vector3Int, DoorSave>(
            StartCoordinates,
            new DoorSave(isOpen, isLocked));

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
                    OpenDoor(null);
                }
            }

        }
    }
}
