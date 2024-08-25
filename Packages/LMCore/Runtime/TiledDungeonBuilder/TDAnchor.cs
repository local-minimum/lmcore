using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
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

    public class TDAnchor : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) =>
            $"Anchor {Anchor} @ {Node.Coordinates}: {message}";

        [Tooltip("Use None for center of cube")]
        public Direction Anchor;

        [HideInInspector]
        public AnchorYRotation PrefabRotation; 

        TDNode _node;
        public TDNode Node
        {
            get { 
                if (_node == null)
                {
                    _node = GetComponentInParent<TDNode>();
                }
                return _node;
            }

            set { _node = value; }
        }

        TiledDungeon _dungeon;
        public TiledDungeon Dungeon
        {
            get {
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<TiledDungeon>();
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

        public TDMovingPlatform ManagingPlatform { get; set; }

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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(CenterPosition, Vector3.one * 0.3f);

            Gizmos.color = Color.blue;
            var a = PrefabRotation.Rotate(Anchor);
            foreach (var direction in DirectionExtensions.AllDirections)
            {
                if (direction == a || direction == a.Inverse()) continue;
                Gizmos.DrawWireSphere(GetEdgePosition(direction), 0.1f);
            }
        }

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

                var n = Node;
                var offset = PrefabRotation.Rotate(Anchor).AsLookVector3D().ToDirection(halfSize);

                if (n != null)
                {
                    return n.CenterPosition + offset;
                }

                return Vector3.up * halfSize + offset;
            }
        }


        public Vector3 GetEdgePosition(Direction direction)
        {
            var d = PrefabRotation.Rotate(direction);
            if (d == Direction.None || d == Anchor) 
                return CenterPosition;

            if (d == Anchor.Inverse())
            {
                Debug.LogWarning(PrefixLogMessage("Requesting inverse of anchor, returning center"));
                return CenterPosition;
            }

            if (Sentinels.ContainsKey(d)) return Sentinels[d].Position;

            return CenterPosition + direction.AsLookVector3D().ToDirection(HalfGridSize);
        }
        #endregion
    }
}
