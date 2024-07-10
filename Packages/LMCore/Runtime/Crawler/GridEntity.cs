using UnityEngine;
using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

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

        public TransportationMode transportationMode;
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
                        OnFall?.Invoke(true);
                    } else
                    {
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
        List<IEntityMover> movers;
        public IEnumerable<IEntityMover> Movers { 
            get { 
                if (movers == null)
                {
                    movers = GetComponents<IEntityMover>().ToList();
                }
                return movers; 
            } 
        }

        public IEntityMover ActiveMover => movers.Where(m => m.Enabled).FirstOrDefault();
        public CrawlerInput Input => GetComponent<CrawlerInput>();

        #endregion Movers

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

        Vector3 AnchorOffset
        {
            get
            {
                // TODO: Respect when rotation respects anchor
                if (Anchor == Direction.Down) return Vector3.zero;
                if (Anchor.IsPlanarCardinal())
                {
                    return wallAnchorOffset * 0.5f * GridSizeProvider.GridSize * Anchor.AsLookVector3D().ToDirection()
                        + Vector3.up * 0.5f * GridSizeProvider.GridSize;

                }

                return ceilingAnchorOffset * GridSizeProvider.GridSize * Anchor.AsLookVector3D().ToDirection();
            }
        }

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
                transportationMode = transportationMode.AddFlag(TransportationMode.Climbing);
            } else
            {
                transportationMode = transportationMode.RemoveFlag(TransportationMode.Climbing);
            }
        }

        public void Interact()
        {
            Debug.Log($"I, {name}, am interacting");
            OnInteract?.Invoke(this);
        }
    }
}
