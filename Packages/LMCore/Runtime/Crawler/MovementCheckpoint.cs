using LMCore.Extensions;
using UnityEngine;

namespace LMCore.Crawler
{
    public class MovementCheckpoint
    {
        private Anchor _anchor;
        public Anchor Anchor { 
            get => _anchor; 
            set
            {
                _anchor = value;
                _node = null;
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
                _node = value;
                _anchor = null;
            }
        }

        Vector3Int _coordinates;
        public Vector3Int Coordinates { 
            get
            {
                if (_anchor != null) return _anchor.Node.Coordinates;
                if (_node != null) return _node.Coordinates;
                return _coordinates;
            }
            set
            {
                _coordinates = value;
                _anchor = null;
                _node = null;
            }
        }

        public AnchorTraversal Traversal
        {
            get
            {
                if (_anchor != null) return _anchor.Traversal;

                return AnchorTraversal.None;
            }
        }

        public Direction Down
        {
            get
            {
                if (_anchor != null)
                {
                    return _anchor.CubeFace;
                }

                return Edge;
            }
        }

        public Vector3 Position(IDungeon dungeon)
        {
            if (_anchor == null)
            {
                if (Edge == Direction.None)
                {
                    return _anchor.CenterPosition;
                }

                return _anchor.GetEdgePosition(Edge);
            }

            if (_node != null)
            {
                if (Edge == Direction.None)
                {
                    return _node.CenterPosition;
                }

                return _node.GetEdge(Edge);
            }

            return _coordinates.ToPosition(dungeon.GridSize) + Edge.AsLookVector3D().ToDirection(dungeon.GridSize * 0.5f);
        }

        public bool IsSame(MovementCheckpoint other)
        {
            if (other == null) return false;

            if (Anchor != null && other.Anchor != null) return Anchor == other.Anchor && Edge == other.Edge;
            if (Node != null && other.Node != null) return Node == other.Node && Edge == other.Edge;

            return Coordinates == other.Coordinates && Edge == other.Edge;
        }

        public Direction Edge { get; set; }

        public static MovementCheckpoint From(GridEntity entity)
        {
            if (entity.NodeAnchor != null)
            {
                return new MovementCheckpoint { Anchor = entity.NodeAnchor, Edge = Direction.None };
            }
            else if (entity.Node != null)
            {
                return new MovementCheckpoint { Node = entity.Node, Edge = entity.Anchor };
            }

            return new MovementCheckpoint { Coordinates = entity.Coordinates, Edge = entity.Anchor };
        }

        public static MovementCheckpoint From(IDungeonNode node, Direction edge = Direction.None) =>
            new MovementCheckpoint { Node = node, Edge = edge };

        public static MovementCheckpoint FromAnchor(IDungeonNode node, Direction anchorDirection) =>
            new MovementCheckpoint
            {
                Anchor = node.GetAnchor(anchorDirection),
                Edge = Direction.None
            };

        public static MovementCheckpoint From(Vector3Int coordinates, Direction edge = Direction.None) =>
            new MovementCheckpoint
            {
                Coordinates = coordinates,
                Edge = edge
            };

        public static MovementCheckpoint From(MovementCheckpoint template) =>
            From(template, template.Edge);

        public static MovementCheckpoint From(MovementCheckpoint template, Direction edge) {
            if (template.Anchor != null)
            {
                return new MovementCheckpoint { Anchor = template.Anchor, Edge = edge };
            }

            if (template.Node != null)
            {
                return new MovementCheckpoint { Node = template.Node, Edge = edge };
            }

            return new MovementCheckpoint
            {
                Coordinates = template.Coordinates,
                Edge = edge
            };
        }

        public static MovementCheckpoint From(Anchor anchor, Direction edge = Direction.None) =>
            new MovementCheckpoint { Anchor = anchor, Edge = edge };
    }
}
