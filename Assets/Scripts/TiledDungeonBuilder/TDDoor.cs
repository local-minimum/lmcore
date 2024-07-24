using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TiledImporter;
using TiledDungeon.Integration;
using TiledDungeon.Actions;

namespace TiledDungeon
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

        bool automatic;
        GridEntity trapTriggeringEntity;

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

        bool AutomaticTrapdoorAction(GridEntity entity, List<Vector3Int> positions) =>
            automatic
                && !entity.TransportationMode.HasFlag(TransportationMode.Climbing)
                && !entity.TransportationMode.HasFlag(TransportationMode.Flying)
                && positions.Contains(Position);

        IEnumerator<WaitForSeconds> AutoClose(string logMessage)
        {
            yield return new WaitForSeconds(autoCloseTime);
            if (isOpen || ActiveTransition == Transition.Opening)
            {
                Interact();
                if (!string.IsNullOrEmpty(logMessage)) Debug.Log(logMessage);
            }
            trapTriggeringEntity = null;
        }

        private void Mover_OnMoveStart(GridEntity entity, List<Vector3Int> positions, List<Direction> anchors)
        {
            if (AutomaticTrapdoorAction(entity, positions))
            {
                if ((isOpen || ActiveTransition == Transition.Opening) && entity == trapTriggeringEntity && positions.Last() != Position)
                {
                    StartCoroutine(AutoClose($"Door @ {Position} automatically closes after {entity.name}"));
                }
                else if (!isOpen && positions.First() != Position && positions.Last() == Position)
                {
                    Interact();
                    trapTriggeringEntity = entity;
                    Debug.Log($"Door @ {Position} automatically opens for {entity.name}");
                }
                else {
                    Debug.Log($"No door action for Open({isOpen}) door");
                }
            }

            activelyMovingEntities.Add(entity);
        }

        HashSet<GridEntity> activelyMovingEntities = new();

        private void Mover_OnMoveEnd(GridEntity entity,  bool successful)
        {
            activelyMovingEntities.Remove(entity);
            if (successful && AutomaticTrapdoorAction(entity, new List<Vector3Int>() { entity.Position }))
            {
                entity.Falling = true; 
            } else if (!successful && trapTriggeringEntity == entity)
            {
                StartCoroutine(AutoClose("Door closed because move failed"));
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
                        .GetComponentsInChildren<IInventory>()
                        .FirstOrDefault(i => i.HasItem(TiledConfiguration.instance.KeyKey, key));

                    if (keyHolder == null)
                    {
                        Debug.LogWarning($"Door requires key ({key})");
                        return;
                    }

                    if (consumesKey && !keyHolder.Consume(TiledConfiguration.instance.KeyKey, key))
                    {
                        Debug.LogWarning($"Failed to consume key {key} from {keyHolder}");
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

        [ContextMenu("Interact")]
        public void Interact()
        {
            Debug.Log($"Toggling door at {Position} from Open({isOpen} / {ActiveTransition})");
            var transition = ActiveTransition;
            if (transition != Transition.None)
            {
                Debug.Log("Resque from previous action");
                MapOverActions(transition == Transition.Opening ? OpenActions : CloseActions, (action) => action.Abandon());
                MapOverActions(transition == Transition.Opening ? CloseActions : OpenActions, (action) => action.PlayFromCurrentProgress(
                    () => isOpen = transition == Transition.Closing));

            } else
            {
                var endsOpen = !isOpen;
                MapOverActions(isOpen ? CloseActions : OpenActions, (action) => action.Play(() => isOpen = endsOpen));
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

            automatic = node.GetObjectValues(
                TiledConfiguration.instance.TrapDoorClass,
                props => props.Bool(TiledConfiguration.instance.ObjAutomaticKey)
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
                $"Syncing door @ {Position}: Locked({isLocked}) Key({key}; consumes={consumesKey}) Open({isOpen}) Automatic({automatic})"
            );
        }
    }
}
