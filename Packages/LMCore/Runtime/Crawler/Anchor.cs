using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public enum AnchorTraversal { None, Walk, Climb, Scale, Stairs, Converyor }
    public enum AnchorYRotation { None, CW, CCW, OneEighty }

    public static class AnchorYRotationExtensions
    {
        public static AnchorYRotation AsYRotation(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return AnchorYRotation.None;
                case Direction.South:
                    return AnchorYRotation.OneEighty;
                case Direction.East:
                    return AnchorYRotation.CW;
                case Direction.West:
                    return AnchorYRotation.CCW;
                default:
                    Debug.LogError($"Can't construction Y rotation from {direction}");
                    return AnchorYRotation.None;
            }
        }

        public static Direction Rotate(this AnchorYRotation rotation, Direction direction) { 
            if (!direction.IsPlanarCardinal()) { return direction; }

            switch (rotation)
            {
                case AnchorYRotation.None:
                    return direction;
                case AnchorYRotation.CW:
                    return direction.RotateCW();
                case AnchorYRotation.CCW:
                    return direction.RotateCCW();
                default:
                    return direction.Inverse();
            }
        }
    }

    public class Anchor : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) =>
            $"Anchor {CubeFace} @ {Node.Coordinates}: {message}";

        [HideInInspector]
        public AnchorYRotation PrefabRotation = AnchorYRotation.None;

        public AnchorTraversal Traversal;

        [SerializeField, Tooltip("Use None for center of cube")]
        private Direction _PrefabCubeFace = Direction.Down;

        public bool HasEdge(Direction direction) =>
            CubeFace != direction && CubeFace.Inverse() != direction;

        public Direction CubeFace =>
            PrefabRotation.Rotate(_PrefabCubeFace);

        IDungeonNode _node;
        public IDungeonNode Node
        {
            get { 
                if (_node == null)
                {
                    _node = GetComponentInParent<IDungeonNode>();
                }
                return _node;
            }

            set { _node = value; }
        }

        public override string ToString() =>
            $"Anchor {CubeFace} ({Traversal}) of {Node}";

        IDungeon _dungeon;
        public IDungeon Dungeon
        {
            get {
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<IDungeon>();
                }
                return _dungeon;
            }

            set { _dungeon = value; }
        }

        float HalfGridSize
        {
            get
            {
                var d = Dungeon;
                if (d == null) return 1.5f;

                return d.GridSize * 0.5f;
            }
        }

        public IMovingCubeFace ManagingMovingCubeFace { get; set; }


        #region Managing Entiteis
        private HashSet<GridEntity> entities = new HashSet<GridEntity>();
        public void AddAnchor(GridEntity entity) =>
            entities.Add(entity);

        public void RemoveAnchor(GridEntity entity) => 
            entities.Remove(entity);
        #endregion

        #region WorldPositions
        Dictionary<Direction, PositionSentinel> _sentinels = null;
        Dictionary<Direction, PositionSentinel> Sentinels
        {
            get
            {
                if (_sentinels == null)
                {
                    _sentinels = new Dictionary<Direction, PositionSentinel>(
                        GetComponentsInChildren<PositionSentinel>()
                        .Select(s => new KeyValuePair<Direction, PositionSentinel>(s.Direction, s))
                    );
                }

                return _sentinels;
            }
        }

#if UNITY_EDITOR
        float edgeDirectionToSize(Direction direction)
        {
            switch (direction)
            {                
                case Direction.North:
                case Direction.East:
                case Direction.Up:
                    return 0.1f;
                case Direction.Down:
                case Direction.West:
                case Direction.South:
                    return 0.075f;
                default:
                    return 0.05f;
            }
        }

        Dictionary<Direction, Color> directionToColor = new Dictionary<Direction, Color>() {
            { Direction.North, Color.blue },
            { Direction.South, Color.white},
            { Direction.West, Color.green },
            { Direction.East, Color.yellow },
            { Direction.Up, Color.cyan},
            { Direction.Down, Color.magenta },
            { Direction.None, Color.red },
        };

        private void OnDrawGizmosSelected()
        {
            var a = CubeFace;
            Gizmos.color = directionToColor[a];
            Gizmos.DrawWireCube(CenterPosition, Vector3.one * 0.3f);

            foreach (var direction in DirectionExtensions.AllDirections)
            {
                if (!HasEdge(direction)) continue;
                Gizmos.color = directionToColor[direction];
                Gizmos.DrawWireSphere(GetEdgePosition(direction), edgeDirectionToSize(direction));
            }
        }
#endif

        [ContextMenu("Refresh sentinels in editor")]
        void RefreshSentinels() => _sentinels = null;

        public Vector3 CenterPosition { 
            get
            {
                var halfSize = HalfGridSize;
                var s = Sentinels;
                if (s != null && s.ContainsKey(Direction.None))
                {
                    return s[Direction.None].Position;
                }

                var offset = CubeFace.AsLookVector3D().ToDirection(halfSize);

                if (ManagingMovingCubeFace != null)
                {
                    return ManagingMovingCubeFace.VirtualNodeCenter + offset;
                }

                var n = Node;
                if (n != null)
                {
                    return n.CenterPosition + offset;
                }

                return Vector3.up * halfSize + offset;
            }
        }


        public Vector3 GetEdgePosition(Direction direction)
        {
            if (direction == Direction.None || direction == CubeFace) 
                return CenterPosition;

            if (direction == CubeFace.Inverse())
            {
                Debug.LogWarning(PrefixLogMessage("Requesting inverse of anchor, returning center"));
                return CenterPosition;
            }

            if (Sentinels.ContainsKey(direction)) return Sentinels[direction].Position;

            return CenterPosition + direction.AsLookVector3D().ToDirection(HalfGridSize);
        }
        #endregion

        public Anchor GetNeighbour(Direction direction, out bool sameNode)
        {
            if (!HasEdge(direction))
            {
                sameNode = true;
                return null;
            }

            var node = Node;
            if (node != null)
            {
                var neighbourAnchor = node.GetAnchor(direction);
                if (neighbourAnchor != null)
                {
                    sameNode = true;
                    return neighbourAnchor;
                }

                // TODO: We need to check if entity can exit the node this way
                // or it has to be checked somewhere else
                var neighbourCoordinates = node.Neighbour(direction);
                var dungeon = Dungeon;
                if (dungeon != null && dungeon.HasNodeAt(neighbourCoordinates))
                {
                    var neighbourNode = Dungeon[neighbourCoordinates];
                    if (neighbourNode != null )
                    {
                        // TODO: We need to check if entity can enter this
                        // face from the direction in question
                        sameNode = false;
                        return neighbourNode.GetAnchor(CubeFace);
                    }
                }
            }

            sameNode = true;
            return null;
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(PrefixLogMessage($"Managed entities: {string.Join(", ", entities.Select(e => e.name))}"));
        }
    }
}
