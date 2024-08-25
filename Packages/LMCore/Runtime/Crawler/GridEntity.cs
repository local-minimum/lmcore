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

        protected string PrefixLogMessage(string message) => $"Entity '{name}' @ {Coordinates} anchor {Anchor} looking {LookDirection}: {message}";

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

        #region Coordinates
        /// <summary>
        /// Using XZ Plane, returns coordinates in 2D
        /// </summary>
        public Vector2Int Coordinates2D {
            get => Coordinates.To2DInXZPlane();
            set => Coordinates = value.To3DFromXZPlane(Elevation);
        }

        public int Elevation
        {
            get => Coordinates.y;
            set
            {
                _Coordinates.y = value;
            }
        }

        Vector3Int _Coordinates;
        public Vector3Int Coordinates
        {
            get => _Coordinates;
            set => _Coordinates = value;
        }
        #endregion

        public Direction LookDirection { get; set; }

        /// <summary>
        /// Updates position and rotation as well as occupying dungeon node at coordinates possible
        /// </summary>
        public void Sync()
        {
            if (GridSizeProvider == null)
            {
                Debug.LogWarning(PrefixLogMessage("have yet to recieve a grid size provider, ignoring sync"));
                return;
            }

            transform.position = Dungeon.Position(this);
            transform.rotation = LookDirection.AsQuaternion(Anchor, RotationRespectsAnchorDirection);

            var node = Dungeon[Coordinates];
            if (node != null)
            {
                node.AddOccupant(this);
            }
            CheckFall();
        }

        /// <summary>
        /// Update coordinates based on a movement.
        /// 
        /// Does not sync position
        /// </summary>
        public void Translate(Movement movement)
        {
            Coordinates = LookDirection.RelativeTranslation3D(Anchor, movement).Translate(Coordinates);
        }

        /// <summary>
        /// Update look direction and transportation mode climbing based on movement.
        /// 
        /// Does not sync rotation
        /// </summary>
        public void Rotate(Movement movement)
        {
            LookDirection = LookDirection.ApplyRotation(Anchor, movement, out Anchor);

            if (Anchor != Direction.Down && Anchor != Direction.None)
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

            var node = Dungeon[Coordinates];
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
