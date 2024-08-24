using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDAnchor : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) =>
            $"Anchor {Anchor} @ {Node.Coordinates}: {message}";

        [SerializeField, HideInInspector]
        Direction Anchor;

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
        // TODO: This isn't great, we need a better way to say where we are
        public Vector3 CenterPosition =>
            transform.position;


        public Vector3 GetEdgePosition(Direction direction)
        {
            if (direction == Direction.None || direction == Anchor) 
                return CenterPosition;

            if (direction == Anchor.Inverse())
            {
                Debug.LogWarning(PrefixLogMessage("Requesting inverse of anchor, returning center"));
                return CenterPosition;
            }

            return CenterPosition + direction.AsLookVector3D().ToDirection(Dungeon.GridSize);
        }
        #endregion
    }
}
