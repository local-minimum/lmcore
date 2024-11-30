using LMCore.Crawler;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledImporter;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDAreaMarker : TDFeature
    {
        [SerializeField]
        string _EntityId;
        public string EntityId => _EntityId;
        
        public void Configure(TiledCustomProperties props)
        {
            _EntityId = props.String(
                TiledConfiguration.instance.ObjEnemyIdKey,
                TiledConfiguration.instance.NPCClass);
        }
        public Direction Direction =>
            Anchor ? Anchor.CubeFace : Direction.None;

        bool MatchingDirection(Direction direction) =>
            Direction == direction || Direction == Direction.None;

        public bool AtArea(GridEntity entity) =>
            EntityId == entity.Identifier && entity.Coordinates == Coordinates && MatchingDirection(entity.AnchorDirection);
        public bool AtArea(TDEnemy enemy) =>
            enemy.Id == EntityId && enemy.Entity.Coordinates == Coordinates && MatchingDirection(enemy.Entity.AnchorDirection);

        static List<TDAreaMarker> Areas = new List<TDAreaMarker>();

        private void OnEnable()
        {
            Areas.Add(this);
        }

        private void OnDisable()
        {
            Areas.Remove(this);
        }

        private void OnDestroy()
        {
            Areas.Remove(this);
        }

        public static IEnumerable<TDAreaMarker> GetAll(GridEntity entity) =>
            Areas
                .Where(a => a.EntityId == entity.Identifier);

        public static IEnumerable<TDAreaMarker> GetAll(TDEnemy enemy) =>
            Areas
                .Where(a => a.EntityId == enemy.Id);
    }
}
