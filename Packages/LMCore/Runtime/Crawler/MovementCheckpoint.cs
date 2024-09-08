using LMCore.Extensions;
using UnityEngine;

namespace LMCore.Crawler
{
    public class MovementCheckpoint
    {
        public Direction LookDirection { get; set; }

        public Quaternion Rotation(GridEntity entity)
        {
            if (LookDirection == Direction.None) {
                Debug.LogError($"{entity.name} Checkpoint: LookDirection is None, can't produce rotation");
                return entity.transform.rotation;
            }
            
            try
            {
                return LookDirection.AsQuaternion(Down, entity.RotationRespectsAnchorDirection);
            } catch (System.ArgumentException e)
            {
                Debug.LogError($"{entity.name} Checkpoint: LookDirection can't be turned into 2d vector: {e.Message}");
                return entity.transform.rotation;
            }
        }

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
            if (_anchor != null)
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

            return Coordinates == other.Coordinates && Edge == other.Edge && LookDirection == other.LookDirection;
        }

        public Direction Edge { get; set; }

        public Direction AnchorDirection
        {
            get
            {
                if (Anchor == null) return Edge;

                return Anchor.CubeFace;
            }
        }

        private string Attachment
        {
            get
            {
                if (Anchor != null) return "Anchor";
                if (Node != null) return "Node";
                return "Dungeon";
            }
        }

        public override string ToString() =>
            $"{Coordinates} - Attached({Attachment}) Traversal({Traversal}) Anchor({AnchorDirection}) Down({Down}) Edge({Edge}) Looking({LookDirection})";

        #region Creation
        public static MovementCheckpoint From(GridEntity entity)
        {
            if (entity.NodeAnchor != null)
            {
                return new MovementCheckpoint {
                    Anchor = entity.NodeAnchor, 
                    Edge = Direction.None,
                    LookDirection = entity.LookDirection,
                };
            }
            else if (entity.Node != null)
            {
                return new MovementCheckpoint { 
                    Node = entity.Node, 
                    Edge = entity.Anchor,
                    LookDirection = entity.LookDirection,
                };
            }

            return new MovementCheckpoint { 
                Coordinates = entity.Coordinates, 
                Edge = entity.Anchor,
                LookDirection = entity.LookDirection,
            };
        }

        public static MovementCheckpoint From(
            IDungeonNode node, 
            Direction edge,
            Direction lookDirection 
            ) =>
            new MovementCheckpoint { Node = node, Edge = edge, LookDirection = lookDirection };

        public static MovementCheckpoint FromAnchor(
            IDungeonNode node, 
            Direction anchorDirection,
            Direction lookDirection
            ) =>
            new MovementCheckpoint
            {
                Anchor = node.GetAnchor(anchorDirection),
                Edge = Direction.None,
                LookDirection = lookDirection,
            };

        public static MovementCheckpoint From(
            Vector3Int coordinates, 
            Direction edge = Direction.None,
            Direction lookDirection = Direction.North
            ) =>
            new MovementCheckpoint
            {
                Coordinates = coordinates,
                Edge = edge,
                LookDirection = lookDirection,
            };

        public static MovementCheckpoint From(MovementCheckpoint template) =>
            From(template, template.Edge, template.LookDirection);

        public static MovementCheckpoint From(MovementCheckpoint template, Direction edge, Direction lookDirection) {
            if (template.Anchor != null)
            {
                return new MovementCheckpoint { 
                    Anchor = template.Anchor,
                    Edge = edge,
                    LookDirection = lookDirection,
                };
            }

            if (template.Node != null)
            {
                return new MovementCheckpoint { 
                    Node = template.Node, 
                    Edge = edge,
                    LookDirection = lookDirection,
                };
            }

            return new MovementCheckpoint
            {
                Coordinates = template.Coordinates,
                Edge = edge,
                LookDirection = lookDirection,
            };
        }

        public static MovementCheckpoint From(
            Anchor anchor,
            Direction edge,
            Direction lookDirection) =>
            new MovementCheckpoint {
                Anchor = anchor,
                Edge = edge,
                LookDirection = lookDirection,
            };
        #endregion
    }
}
