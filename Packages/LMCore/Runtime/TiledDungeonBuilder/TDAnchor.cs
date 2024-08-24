using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDAnchor : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) =>
            $"Anchor {Anchor} @ {Node.Coordinates}: {message}";

        [HideInInspector, Tooltip("Use None for center")]
        public Direction Anchor;

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
        Dictionary<Direction, PositionSentinel> sentinels
        {
            get
            {
                if (sentinels == null)
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
            Gizmos.DrawCube(CenterPosition, Vector3.one * 0.1f);
        }

        public Vector3 CenterPosition =>
            sentinels.ContainsKey(Direction.None) ?
            sentinels[Direction.None].Position :
            Node.CenterPosition + Anchor.AsLookVector3D().ToDirection(Dungeon.GridSize * 0.5f);


        public Vector3 GetEdgePosition(Direction direction)
        {
            if (direction == Direction.None || direction == Anchor) 
                return CenterPosition;

            if (direction == Anchor.Inverse())
            {
                Debug.LogWarning(PrefixLogMessage("Requesting inverse of anchor, returning center"));
                return CenterPosition;
            }

            if (sentinels.ContainsKey(direction)) return sentinels[direction].Position;

            return CenterPosition + direction.AsLookVector3D().ToDirection(Dungeon.GridSize * 0.5f);
        }
        #endregion
    }
}
