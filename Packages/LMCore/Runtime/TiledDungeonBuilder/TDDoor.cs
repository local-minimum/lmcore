using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Inventory;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.Actions;

namespace LMCore.TiledDungeon
{
    // TODO: Use configuration to present lock and button
    public class TDDoor : MonoBehaviour
    {
        private enum Transition { None, Opening, Closing };

        [SerializeField, HideInInspector]
        bool isOpen = false;

        [SerializeField, HideInInspector]
        Vector3Int Position;

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
                    Debug.Log($"Door at {Position} is closing");
                    return true;
                }

                return !isOpen;
            }
        }


        public DirectionAxis TraversalAxis
        {
            get
            {
                var mod = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.DoorClass);
                if (mod == null) return DirectionAxis.None;

                return mod
                    .Tile
                    .CustomProperties
                    .Orientation(TiledConfiguration.instance.TraversalAxisKey)
                    .AsAxis();
            }
        }

        private void Start()
        {
            if (node != null) SyncDoor();
        }

        public void Configure(
            TDNode node,
            Vector3Int position, 
            TileModification[] modifications
        )
        {
            Position = position;
            this.modifications = modifications;
            this.node = node;

            SyncDoor();
        }

        private void OnEnable()
        {
            GridEntity.OnInteract += GridEntity_OnInteract;

            foreach (var mover in Movers.movers) {
                mover.OnMoveStart += Mover_OnMoveStart;
                mover.OnMoveEnd += Mover_OnMoveEnd;
            }

            Movers.OnActivateMover += Movers_OnActivateMover;
            Movers.OnDeactivateMover += Movers_OnDeactivateMover;
        }


        private void OnDisable()
        {
            GridEntity.OnInteract += GridEntity_OnInteract;

            foreach (var mover in Movers.movers) {
                mover.OnMoveStart -= Mover_OnMoveStart;
                mover.OnMoveEnd -= Mover_OnMoveEnd;
            }

            Movers.OnActivateMover -= Movers_OnActivateMover;
            Movers.OnDeactivateMover -= Movers_OnDeactivateMover;
        }

        bool AutomaticTrapdoorAction(GridEntity entity, List<Vector3Int> positions, List<Direction> anchors, out bool endsOnTrap)
        {
            var states = positions
                .Zip(anchors, (pos, anch) => new { pos, anch })
                .Select(state => state.pos == Position && state.anch == Direction.Down)
                .ToArray();

            endsOnTrap = states.LastOrDefault();
            
            return automaticTrapDoor
                && !entity.TransportationMode.HasFlag(TransportationMode.Climbing)
                && !entity.TransportationMode.HasFlag(TransportationMode.Flying)
                && states.Any(b => b);
        }

        HashSet<GridEntity> trapTriggeringEntities = new HashSet<GridEntity>();

        IEnumerator<WaitForSeconds> AutoClose(string logMessage)
        {
            yield return new WaitForSeconds(autoCloseTime);
            if (isOpen || ActiveTransition == Transition.Opening)
            {
                CloseDoor();
                if (!string.IsNullOrEmpty(logMessage)) Debug.Log(logMessage);
            }
        }

        private void Mover_OnMoveStart(GridEntity entity, List<Vector3Int> positions, List<Direction> anchors)
        {
            activelyMovingEntities.Add(entity);

            if (AutomaticTrapdoorAction(entity, positions, anchors, out bool endsOnTrap))
            {
                if (endsOnTrap)
                {
                    trapTriggeringEntities.Add(entity);
                } else
                {
                    trapTriggeringEntities.Remove(entity);
                }

                if (isOpen || ActiveTransition == Transition.Opening)
                {
                    StartCoroutine(AutoClose($"Door @ {Position} automatically closes after {entity.name}"));
                }
                else if (endsOnTrap)
                {
                    OpenDoor();
                    Debug.Log($"Door @ {Position} automatically opens for {entity.name}");
                }
                else {
                    Debug.Log($"No door action for Open({isOpen}) ActiveTransition({ActiveTransition}) door");
                    return;
                }

            }
        }

        HashSet<GridEntity> activelyMovingEntities = new();

        private void Mover_OnMoveEnd(GridEntity entity, bool successful)
        {
            activelyMovingEntities.Remove(entity);
            if (AutomaticTrapdoorAction(entity, new List<Vector3Int>() { entity.Position }, new List<Direction> { entity.Anchor }, out bool _))
            {
                entity.Falling = true; 
            }
        }
        private void Movers_OnDeactivateMover(IEntityMover mover)
        {
            mover.OnMoveStart -= Mover_OnMoveStart;
            mover.OnMoveEnd -= Mover_OnMoveEnd;
        }

        private void Movers_OnActivateMover(IEntityMover mover)
        {
            mover.OnMoveStart += Mover_OnMoveStart;
            mover.OnMoveEnd += Mover_OnMoveEnd;
        }

        private void GridEntity_OnInteract(GridEntity entity)
        {
            var onTheMove = activelyMovingEntities.Contains(entity);
            var validPosition = entity.LookDirection.Translate(entity.Position) == Position;

            if (!onTheMove && validPosition)
            {
                Debug.Log("Attempting to open door");

                if (isLocked)
                {
                    var keyHolder = entity
                        .GetComponentsInChildren<AbsInventory>()
                        .FirstOrDefault(i => i.HasItem(TiledConfiguration.instance.KeyKey, key));

                    if (keyHolder == null)
                    {
                        Debug.LogWarning($"Door @ {Position}: requires key ({key})");
                        return;
                    }

                    if (consumesKey && !keyHolder.Consume(TiledConfiguration.instance.KeyKey, key))
                    {
                        Debug.LogWarning($"Chest @ {Position}: Failed to consume key {key} from {keyHolder}");
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
            Debug.Log($"Toggling door at {Position} from Open({isOpen} / {ActiveTransition})");
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
                    Debug.LogError($"Door at {Position} has unhandled transition: {ActiveTransition}");
                    break;
            }
        }

        void SyncDoor()
        {
            var toggleGroups = node
                .GetObjectValues(
                    TiledConfiguration.instance.ObjToggleGroupClass,
                    props => props.Int(TiledConfiguration.instance.ObjGroupKey)
                )
                .Where(group => group > 0)
                .ToHashSet();

            foreach (var toggleGroup in toggleGroups)
            {
                ToggleGroup.instance.RegisterReciever(toggleGroup, Interact);
            }

            automaticTrapDoor = node.GetObjectValues(
                TiledConfiguration.instance.TrapDoorClass,
                props => props.Interaction(TiledConfiguration.instance.InteractionKey) == TDEnumInteraction.Automatic
            ).Any();

            isOpen = node.FirstObjectValue(
                TiledConfiguration.instance.ObjInitialClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.OpenKey)
            );

            MapOverActions(isOpen ? OpenActions : CloseActions, (action) => {
                action.Play(null);
                action.Finalise();
            });

            isLocked = modifications.Any(
                mod => mod.Tile.CustomProperties.Interaction(TiledConfiguration.instance.InteractionKey) == TDEnumInteraction.Locked
            );

            key = node.FirstObjectValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props?.String(TiledConfiguration.instance.KeyKey)
            );
            consumesKey = node.FirstObjectValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.ConusumesKeyKey)
            );

            Debug.Log(
                $"Syncing door @ {Position}: Locked({isLocked}) Key({key}; consumes={consumesKey}) Open({isOpen}) Automatic({automaticTrapDoor})"
            );
        }
    }
}
