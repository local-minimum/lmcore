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

        protected string PrefixLogMessage(string message) => $"Entity '{name}' @ {Position} anchor {Anchor} looking {LookDirection}: {message}";

        private bool _falling;
        public bool Falling
        {
            get => _falling;
            set {
                if (value != _falling)
                {
                    if (value) {
                        Debug.Log(PrefixLogMessage("Is falling"));
                        OnFall?.Invoke(true);
                    } else
                    {
                        Debug.Log(PrefixLogMessage("Stopped falling"));
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
                Debug.LogWarning(PrefixLogMessage("have yet to recieve a grid size provider, ignoring sync"));
                return;
            }

            transform.position = Dungeon.Position(this);
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
            Debug.Log(PrefixLogMessage("Interacting"));
            OnInteract?.Invoke(this);
        }

        public void CheckFall()
        {
            if (TransportationMode.HasFlag(TransportationMode.Flying) || TransportationMode.HasFlag(TransportationMode.Climbing))
            {
                Debug.Log(PrefixLogMessage("Ended its fall due to tranportation mode"));
                Falling = false;
                return;
            }

            var node = Dungeon[Position];
            if (node == null) {
                Debug.LogWarning(PrefixLogMessage("Outside the map, assuming fall"));
                Falling = true;
                return;
            }

            if (!TransportationMode.HasFlag(TransportationMode.Flying) && !node.CanAnchorOn(this, Anchor))
            {
                if (!node.HasFloor)
                {
                    Debug.Log(PrefixLogMessage("In the air -> falling"));
                    Falling = true;
                }
            } else if (Falling)
            {
                Debug.Log(PrefixLogMessage("Ended its fall"));
                Falling = false;
            }
        }
    }
}
