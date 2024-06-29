using LMCore.Crawler;
using LMCore.Juice;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TiledImporter;

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

        [SerializeField, HideInInspector]
        TiledObjectLayer.Point[] Points;

        [SerializeField, HideInInspector]
        TiledObjectLayer.Rect[] Rects;

        bool isLocked;

        string key;

        private void Start()
        {
            SyncDoor();
        }

        public void Configure(Vector3Int position, TileModification[] modifications, TiledObjectLayer.Point[] points, TiledObjectLayer.Rect[] rects)
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

            // Debug.Log($"onTheMove({onTheMove}) validPosition({validPosition}) {Position} vs {entity.Position}");
            if (!onTheMove && validPosition)
            {
                Debug.Log("Attempting to open door");

                if (isLocked)
                {
                    Debug.LogWarning($"Door requires key ({key})");
                    return;
                }
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

        float SyncOpenness
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

            SyncOpenness = isOpen ? 1 : 0;
            isLocked = modifications.Any(
                mod => mod.Tile.CustomProperties.StringEnums.GetValueOrDefault("Interaction")?.Value == "Locked"
            );

            key = Points
                .FirstOrDefault(p => p.Name == "Lock")
                ?.CustomProperties
                .Strings
                .GetValueOrDefault("Key")
                ?? Rects
                    .FirstOrDefault(r => r.Name == "Lock")
                    ?.CustomProperties
                    .Strings
                    .GetValueOrDefault("Key");

            Debug.Log($"Syncing door @ {Position}: Locked({isLocked}) Key({key}) Open({isOpen})");
        }


        private void Update()
        {
            if (DoorSliding.IsEasing)
            {
                SyncOpenness = DoorSliding.Evaluate();
                if (!DoorSliding.IsEasing)
                {
                    isOpen = DoorSliding.Evaluate() == 1;
                }
            }
        }
    }
}
