using UnityEngine;
using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using LMCore.AbstractClasses;

namespace LMCore.Crawler
{
    public delegate void InteractEvent(GridEntity entity);

    public class GridEntity : MonoBehaviour
    {
        public static event InteractEvent OnInteract;

        public UnityEvent<bool> OnFall;
        public UnityEvent ContinueFall;
        public UnityEvent OnLand;

        public GridEntityType EntityType;
        public IGridSizeProvider GridSizeProvider { get; set; }

        public TransportationMode TransportationMode;
        public Direction Anchor = Direction.Down;
        public bool RotationRespectsAnchorDirection { get; set; } = false;

        private bool falling;
        public bool Falling
        {
            get => falling;
            set {
                if (value != falling)
                {
                    if (value) {
                        Debug.Log($"{name} is falling");
                        OnFall?.Invoke(true);
                    } else
                    {
                        Debug.Log($"{name} stopped falling");
                        OnLand?.Invoke();
                    }
                } else if (value)
                {
                    ContinueFall?.Invoke();
                }
                falling = value;
            }
        }

        #region Movers
        public IEntityMovementInterpreter EntityMovementInterpreter => GetComponent<IEntityMovementInterpreter>();

        public List<IEntityMover> Movers
        {
            get
            {
                return GetComponents<IEntityMover>().ToList();
            }
        }

        #endregion Movers

        public CrawlerInput Input => GetComponent<CrawlerInput>();

        /// <summary>
        /// Using XZ Plane, returns position in 2D
        /// </summary>
        public Vector2Int Position2D {
            get => Position.To2DInXZPlane();
            set => Position = value.To3DFromXZPlane();
        }

        Vector3Int _Position;
        public Vector3Int Position
        {
            get => _Position;
            set => _Position = value;
        }

        [SerializeField]
        Direction StartLookDirection;

        public Direction LookDirection { get; set; }

        private void Start()
        {
            LookDirection = StartLookDirection;
            Sync();
        }

        [SerializeField, Range(0, 1)]
        float wallAnchorOffset = 0.95f;

        [SerializeField, Range(0, 1)]
        float ceilingAnchorOffset = 0.9f;

        public Vector3  CalculateAnchorOffset(Direction anchor, bool rotationRespectsAnchorDirection)
        {
            // TODO: Respect when rotation respects anchor
            if (rotationRespectsAnchorDirection)
            {
                Debug.LogWarning("Anchor offset respecting rotation isn't implemented");
            }

            if (anchor == Direction.Down) return Vector3.zero;
            if (anchor.IsPlanarCardinal())
            {
                return wallAnchorOffset * 0.5f * GridSizeProvider.GridSize * anchor.AsLookVector3D().ToDirection()
                    + Vector3.up * 0.5f * GridSizeProvider.GridSize;

            }

            return ceilingAnchorOffset * GridSizeProvider.GridSize * anchor.AsLookVector3D().ToDirection();

        }

        Vector3 AnchorOffset => CalculateAnchorOffset(Anchor, RotationRespectsAnchorDirection);

        public void Sync()
        {
            if (GridSizeProvider == null)
            {
                Debug.LogError($"{name} have yet to recieve a grid size provider, ignoring sync");
                return;
            }

            transform.position = Position.ToPosition(GridSizeProvider.GridSize) + AnchorOffset;
            transform.rotation = LookDirection.AsQuaternion(Anchor, RotationRespectsAnchorDirection);
        }

        public void Translate(Movement movement)
        {
            Position = LookDirection.RelativeTranslation3D(Anchor, movement).Translate(Position);
        }

        public void Rotate(Movement movement)
        {
            LookDirection = LookDirection.ApplyRotation(Anchor, movement, out Anchor);
            if (Anchor != Direction.Down)
            {
                TransportationMode = TransportationMode.AddFlag(TransportationMode.Climbing);
            } else
            {
                TransportationMode = TransportationMode.RemoveFlag(TransportationMode.Climbing);
            }
        }

        public void Interact()
        {
            Debug.Log($"I, {name}, am interacting");
            OnInteract?.Invoke(this);
        }
    }
}
