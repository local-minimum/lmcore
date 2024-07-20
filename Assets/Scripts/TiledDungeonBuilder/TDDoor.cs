using LMCore.Crawler;
using LMCore.Juice;
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
        /*
        [SerializeField]
        Transform Door;

        [SerializeField]
        float maxSlide = 2.75f;

        [SerializeField]
        TemporalEasing<float> DoorSliding;
        */

        [SerializeField, HideInInspector]
        bool isOpen = false;

        /*
        bool hasSetReferencePosition;
        Vector3 doorReferencePosition;
        */

        [SerializeField, HideInInspector]
        Vector3Int Position;

        [SerializeField, HideInInspector]
        TileModification[] modifications;

        [SerializeField, HideInInspector]
        TiledObjectLayer.Point[] Points;

        [SerializeField, HideInInspector]
        TiledObjectLayer.Rect[] Rects;

        [SerializeField]
        AbstractDungeonAction OpenAction;

        [SerializeField]
        AbstractDungeonAction CloseAction;

        bool isLocked;

        string key;

        bool consumesKey;

        AbstractDungeonAction ActiveAction
        {
            get
            {
                if (OpenAction.IsEasing) return OpenAction;
                if (CloseAction.IsEasing) return CloseAction;
                return null;
            }
        }

        public bool BlockingPassage
        {
            get
            {
                if (ActiveAction == CloseAction) return true;

                return !isOpen;
            }
        }


        public DirectionAxis Axis
        {
            get
            {
                var mod = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.DoorClass);
                if (mod == null) return DirectionAxis.None;

                return mod
                    .Tile
                    .CustomProperties
                    .Orientation(TiledConfiguration.instance.OrientationKey)
                    .AsAxis();
            }
        }

        private void Start()
        {
            SyncDoor();
        }

        public void Configure(
            Vector3Int position, 
            TileModification[] modifications, 
            TiledObjectLayer.Point[] points, 
            TiledObjectLayer.Rect[] rects
        )
        {
            Position = position;
            this.modifications = modifications;
            Points = points;
            Rects = rects;

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

        private void Mover_OnMoveStart(GridEntity entity, List<Vector3Int> positions)
        {
            activelyMovingEntities.Add(entity);
        }

        HashSet<GridEntity> activelyMovingEntities = new();

        private void Mover_OnMoveEnd(GridEntity entity,  bool successful)
        {
            activelyMovingEntities.Remove(entity);
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

        [ContextMenu("Interact")]
        public void Interact()
        {
            Debug.Log($"Toggling door at {Position} from Open({isOpen})");
            if (ActiveAction != null)
            {
                var action =  ActiveAction;
                action.Abandon();
                (action == OpenAction ? CloseAction : OpenAction).PlayFromCurrentProgress(() => isOpen = action == CloseAction);
            } else
            {
                (isOpen ? CloseAction : OpenAction).Play(() => isOpen = !isOpen);
            }
        }

        void SyncDoor()
        {
            if (isOpen)
            {
                OpenAction.Play(null);
                OpenAction.Finalise();
            } else
            {
                CloseAction.Play(null);
                CloseAction.Finalise();
            }

            isLocked = modifications.Any(
                mod => mod.Tile.CustomProperties.Interaction(TiledConfiguration.instance.InteractionKey) == TDEnumInteraction.Locked
            );

            var keyProperties = Points
                .FirstOrDefault(p => p.Name == TiledConfiguration.instance.LockItem)
                ?.CustomProperties
                ?? Rects
                    .FirstOrDefault(r => r.Name == TiledConfiguration.instance.LockItem)
                    ?.CustomProperties;

            if (keyProperties != null )
            {
                key = keyProperties.String(TiledConfiguration.instance.KeyKey);
                consumesKey = keyProperties.Bool(TiledConfiguration.instance.ConusumesKeyKey);
            }

            Debug.Log($"Syncing door @ {Position}: Locked({isLocked}) Key({key}; consumes={consumesKey}) Open({isOpen})");
        }
    }
}
