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
        public EntityAbilities Abilities;
        public MovementInterpreter MovementInterpreter;

        public GridEntityType EntityType;
        public IGridSizeProvider GridSizeProvider { get; set; }
        public IDungeon Dungeon { get; set; }

        private Anchor _anchor;
        public Anchor NodeAnchor {
            get => _anchor;
            set
            {
                _anchor = value;
                if (value != null)
                {
                    _node = null;
                    _anchorDirection = value.CubeFace;
                }
            }
        }

        private Direction _anchorDirection = Direction.Down;
        public Direction Anchor
        {
            get
            {
                if (_anchor != null) return _anchor.CubeFace;
                return _anchorDirection;
            }
            set
            {
                if (Node != null)
                {
                    var anchor = Node.GetAnchor(value);
                    if (anchor != null)
                    {
                        NodeAnchor = anchor;
                        return;
                    }
                }
                _anchorDirection = value;
                _anchor = null;
            }
        }
        
        public Direction Down
        {
            get {
                if (RotationRespectsAnchorDirection && Anchor != Direction.None)
                {
                    return _anchorDirection;
                }

                return Direction.Down; 
            }
        }

        private IDungeonNode _node;
        public IDungeonNode Node { 
            get
            {
                if (_anchor != null) return _anchor.Node;
                return _node;
            }
            set
            {
                var anchor = value?.GetAnchor(Anchor);
                if (anchor != null)
                {
                    NodeAnchor = anchor;
                } else
                {
                    _node = value;
                    _anchor = null;
                    if (value != null) _Coordinates = value.Coordinates;
                }
            }
        }

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
                Coordinates = new Vector3Int(_Coordinates.x, value, _Coordinates.z);
            }
        }

        Vector3Int _Coordinates;
        public Vector3Int Coordinates
        {
            get
            {
                if (_anchor) return _anchor.Node.Coordinates;
                if (_node != null) return _node.Coordinates;
                return _Coordinates;
            }
            set
            {
                _Coordinates = value;
                if (Dungeon.HasNodeAt(_Coordinates))
                {
                    var node = Dungeon[_Coordinates];
                    var anchor = node?.GetAnchor(Anchor);
                    if (anchor != null)
                    {
                        NodeAnchor = anchor;
                    } else
                    {
                        Node = node;
                    }
                } else
                {
                    Node = null;
                }
                Node = Dungeon.HasNodeAt(_Coordinates) ? Dungeon[_Coordinates] : null;
            }
        }
        #endregion

        public TransportationMode TransportationMode;
        public bool RotationRespectsAnchorDirection { get; set; } = false;

        protected string PrefixLogMessage(string message) => $"Entity '{name}' @ {Coordinates} anchor {Anchor} looking {LookDirection}: {message}";

        public override string ToString() => 
            $"Entity '{name}' @ {Coordinates} Anchor({Anchor}) Down({Down}) Looking({LookDirection})";

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
            try
            {
                transform.rotation = LookDirection.AsQuaternion(Down, RotationRespectsAnchorDirection);
            } catch (System.ArgumentException e)
            {
                Debug.LogError(
                    PrefixLogMessage($"Can't parse look direction as rotation ({LookDirection} / 3D {RotationRespectsAnchorDirection}): {e.Message}"));
            }
            var node = Dungeon[Coordinates];
            if (node != null)
            {
                node.AddOccupant(this);
            }
            CheckFall();
        }

        public void Sync(MovementCheckpoint checkpoint)
        {
            if (Node != checkpoint.Node)
            {
                Node?.RemoveOccupant(this);
            }
            if (checkpoint.Anchor != null)
            {
                NodeAnchor = checkpoint.Anchor;
            } else if (checkpoint.Node != null)
            {
                Node = checkpoint.Node;
            } else {
                Coordinates = checkpoint.Coordinates;
            }

            LookDirection = checkpoint.LookDirection;

            Sync();
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
            LookDirection = LookDirection.ApplyRotation(Anchor, movement, out var anchorDirecition);
            Anchor = anchorDirecition;

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
