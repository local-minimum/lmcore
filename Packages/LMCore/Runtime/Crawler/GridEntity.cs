using UnityEngine;
using LMCore.Extensions;
using UnityEngine.Events;
using LMCore.Inventory;

namespace LMCore.Crawler
{
    public delegate void InteractEvent(GridEntity entity);
    public delegate void MoveEvent(GridEntity entity);
    public delegate void PositionTransitionEvent(GridEntity entity);

    [System.Flags]
    public enum MovementType {
        Stationary, 
        Translating,
        Rotating
    };

    public class GridEntity : MonoBehaviour
    {
        public static event InteractEvent OnInteract;
        public static event MoveEvent OnMove;
        public static event PositionTransitionEvent OnPositionTransition;

        /// <summary>
        /// Way to reference the entity in other systems like dialogs and saves
        /// </summary>
        public string Identifier;

        protected string PrefixLogMessage(string message) => $"Entity '{name}' @ {Coordinates} anchor {AnchorDirection}/{AnchorMode} looking {LookDirection}: {message}";

        public override string ToString() => 
            $"Entity '{name}' @ {Coordinates} Anchor({AnchorDirection}/{AnchorMode}) Down({Down}) Looking({LookDirection})";

        [SerializeField]
        private AbsInventory _inventory;
        public AbsInventory Inventory => _inventory;

        public UnityEvent OnFall;
        public UnityEvent ContinueFall;
        public UnityEvent OnLand;
        public EntityAbilities Abilities;
        public MovementInterpreter MovementInterpreter;

        public GridEntityType EntityType;
        public IGridSizeProvider GridSizeProvider { get; set; }
        public IDungeon Dungeon { get; set; }

        private MovementType _moving;
        public MovementType Moving {
            get => _moving; 
            set
            {
                _moving = value;
                OnMove?.Invoke(this);
            }
        }

        #region Anchorage
        public bool RotationRespectsAnchorDirection { get; set; } = false;

        private Anchor _anchor;
        public Anchor NodeAnchor {
            get => _anchor;
            set
            {
                if (_anchor != value) {
                    _anchor?.RemoveAnchor(this);
                }

                // We need to run add occupation checks even within node when changing
                // cube face anchor
                bool newAnchor = _anchor != value;
                bool addOccupant = false;

                if (value != null)
                {
                    value.AddAnchor(this);
                    if (Node != value.Node)
                    {
                        Node?.RemoveOccupant(this);
                        addOccupant = true;
                    }

                    _node = null;
                    _anchorDirection = value.CubeFace;
                    TransportationMode = value.Traversal.ToTransportationMode();
                }
                else
                {
                    // Only keep potential flying mode, we don't fly if we just fall
                    TransportationMode &= TransportationMode.Flying;
                }

                _anchor = value;
                if (addOccupant)
                {
                    // This needs to happen last so that the entity is fully in sync
                    value.Node.AddOccupant(this);
                }
                if (newAnchor) OnPositionTransition?.Invoke(this);
            }
        }

        private Direction _anchorDirection = Direction.Down;
        public Direction AnchorDirection
        {
            get
            {
                if (_anchor != null) return _anchor.CubeFace;
                return _anchorDirection;
            }
            set
            {
                bool newDirection = _anchorDirection != value;
                if (Node != null)
                {
                    var anchor = Node.GetAnchor(value);
                    if (anchor != null)
                    {
                        NodeAnchor = anchor;
                        return;
                    }

                    NodeAnchor?.RemoveAnchor(this);
                }

                _anchorDirection = value;
                _anchor = null;

                if (newDirection) OnPositionTransition?.Invoke(this);
            }
        }
        
        public Direction Down
        {
            get {
                if (RotationRespectsAnchorDirection && AnchorDirection != Direction.None)
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
                bool newNode = false;
                var anchor = value?.GetAnchor(AnchorDirection);
                if (anchor != null)
                {
                    NodeAnchor = anchor;
                } else
                {
                    if (_node != value)
                    {
                        _node?.RemoveOccupant(this);
                        _anchor?.RemoveAnchor(this);
                        newNode = true;
                    }
                    _node = value;
                    _anchor = null;

                    // Only keep potential flying mode, we don't fly if we just fall
                    TransportationMode &= TransportationMode.Flying;
                }

                if (newNode)
                {
                    // This needs to happen last so that the entity is fully in sync
                    value?.AddOccupant(this);
                    OnPositionTransition?.Invoke(this);
                }
            }
        }

        private string AnchorMode
        {
            get {
                if (NodeAnchor != null) return "Achor";
                if (Node != null) return "Node";
                return "Dungeon";
            }
        }
        #endregion

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
                bool newCoords = _Coordinates != value;

                _Coordinates = value;
                if (Dungeon.HasNodeAt(_Coordinates))
                {
                    var node = Dungeon[_Coordinates];
                    var anchor = node?.GetAnchor(AnchorDirection);
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

                if (newCoords) OnPositionTransition?.Invoke(this);
            }
        }
        #endregion

        public Direction LookDirection { get; set; }

        public TransportationMode TransportationMode;


        private bool _falling;
        public bool Falling
        {
            get => _falling;
            set {
                if (value != _falling)
                {
                    if (value) {
                        Debug.Log(PrefixLogMessage("Is falling"));
                        OnFall?.Invoke();
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

        public CrawlerInput Input => GetComponent<CrawlerInput>();

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

            CheckFall();
        }

        public void Sync(MovementCheckpoint checkpoint)
        {
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

            if (!TransportationMode.HasFlag(TransportationMode.Flying) && !node.CanAnchorOn(this, AnchorDirection))
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
