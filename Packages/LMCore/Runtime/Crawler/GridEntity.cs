using UnityEngine;
using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using LMCore.Inventory;

namespace LMCore.Crawler
{
    public delegate void InteractEvent(GridEntity entity);

    public class GridEntity : MonoBehaviour
    {
        public static event InteractEvent OnInteract;

        [SerializeField]
        private AbsInventory _inventory;
        public AbsInventory Inventory => _inventory;

        public UnityEvent<bool> OnFall;
        public UnityEvent ContinueFall;
        public UnityEvent OnLand;

        public GridEntityType EntityType;
        public IGridSizeProvider GridSizeProvider { get; set; }
        public IDungeon Dungeon { get; set; }

        public TransportationMode TransportationMode;
        public Direction Anchor = Direction.Down;
        public bool RotationRespectsAnchorDirection { get; set; } = false;

        private bool _falling;
        public bool Falling
        {
            get => _falling;
            set {
                if (value != _falling)
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
                _falling = value;
            }
        }

        #region Movers
        public IEntityMovementInterpreter EntityMovementInterpreter => GetComponent<IEntityMovementInterpreter>();

        private List<IEntityMover> _movers;
        public List<IEntityMover> Movers
        {
            get
            {
                if (_movers == null )
                {
                    _movers = GetComponents<IEntityMover>().ToList();
                }
                return _movers;
            }
        }
        public IEntityMover ActiveMover => Movers.Where(m => m.Enabled).FirstOrDefault();

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

        public Direction LookDirection { get; set; }

        public void Sync()
        {
            if (GridSizeProvider == null)
            {
                Debug.LogWarning($"{name} have yet to recieve a grid size provider, ignoring sync");
                return;
            }

            transform.position = Position.ToPosition(GridSizeProvider.GridSize) + (
                Dungeon.HasNodeAt(Position) ? 
                Dungeon[Position].AnchorOffset(Anchor, RotationRespectsAnchorDirection) :
                Dungeon.DefaultAnchorOffset(Anchor, RotationRespectsAnchorDirection)
            );
            transform.rotation = LookDirection.AsQuaternion(Anchor, RotationRespectsAnchorDirection);

            var node = Dungeon[Position];
            if (node != null)
            {
                node.AddOccupant(this);
            }
            CheckFall();
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

        public void CheckFall()
        {
            if (TransportationMode.HasFlag(TransportationMode.Flying) || TransportationMode.HasFlag(TransportationMode.Climbing))
            {
                Debug.Log($"{name} ended its fall due to tranportation mode");
                Falling = false;
                return;
            }

            var node = Dungeon[Position];
            if (node == null) {
                Debug.LogWarning($"Player is at {Position}, which is outside the map, assuming fall");
                Falling = true;
                return;
            }

            if (!TransportationMode.HasFlag(TransportationMode.Flying) && !node.CanAnchorOn(this, Anchor))
            {
                if (!node.HasFloor)
                {
                    Debug.Log($"{name} is standing in the air @ {Position} Anchor({Anchor}) Looking({LookDirection}) -> fall");
                    Falling = true;
                }
            } else if (Falling)
            {
                Debug.Log($"{name} ended its fall");
                Falling = false;
            }
        }
    }
}
