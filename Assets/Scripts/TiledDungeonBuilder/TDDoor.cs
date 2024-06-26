using LMCore.Crawler;
using LMCore.Juice;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TiledDungeon
{
    // TODO: Use configuration to present lock and button
    // TODO: Align behaviour with configuration
    public class TDDoor : MonoBehaviour
    {
        [SerializeField]
        Transform Door;

        [SerializeField]
        float maxSlide = 2.75f;

        [SerializeField]
        TemporalEasing<float> DoorSliding;

        [SerializeField, HideInInspector]
        bool isOpen = false;

        bool hasSetReferencePosition;
        Vector3 doorReferencePosition;

        [SerializeField, HideInInspector]
        Vector3Int Position;

        [SerializeField, HideInInspector]
        TileModification[] modifications;

        bool isLocked;

        private void Start()
        {
            SyncDoor();
        }

        public void Configure(Vector3Int position, TileModification[] modifications)
        {
            Position = position;
            this.modifications = modifications;
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

        HashSet<GridEntity> activelyMovingEntities = new();

        private void Mover_OnMoveStart(GridEntity entity, LMCore.IO.Movement movement, Vector3Int startPosition, Direction startDirection, Vector3Int endPosition, Direction endDirection, bool allowed)
        {
            activelyMovingEntities.Add(entity);
        }

        private void Mover_OnMoveEnd(GridEntity entity, LMCore.IO.Movement movement, Vector3Int startPosition, Direction startDirection, Vector3Int endPosition, Direction endDirection, bool allowed)
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
                Interact();
            }
        }

        [ContextMenu("Interact")]
        public void Interact()
        {
            if (Door == null) { return; }

            Debug.Log($"Toggling door at {Position} from Open({isOpen})");

            if (DoorSliding.IsEasing)
            {
                DoorSliding.AbortEase();
            } else if (isOpen)
            {
                DoorSliding.EaseEndToStart();
            } else 
            {
                DoorSliding.EaseStartToEnd();
            }
        }

        float Openness
        {
            set
            {
                Door.transform.position = doorReferencePosition + Door.transform.right * Mathf.Clamp01(value) * -maxSlide;
            }
        }

        void SyncDoor()
        {
            if (!hasSetReferencePosition) { 
                doorReferencePosition = Door.position;
                hasSetReferencePosition = true;
            }

            Openness = isOpen ? 1 : 0;
            isLocked = modifications.Any(
                mod => mod.Tile.CustomProperties.StringEnums.GetValueOrDefault("Interaction")?.Value == "Locked"
            );
            Debug.Log($"Syncing door @ {Position}: Locked({isLocked}) Open({isOpen})");
        }


        private void Update()
        {
            if (DoorSliding.IsEasing)
            {
                Openness = DoorSliding.Evaluate();
                if (!DoorSliding.IsEasing)
                {
                    isOpen = DoorSliding.AtEnd;
                }
            }
        }
    }
}
