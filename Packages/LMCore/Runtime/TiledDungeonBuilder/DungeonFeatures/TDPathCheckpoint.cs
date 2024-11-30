using LMCore.Crawler;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledImporter;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDPathCheckpoint : TDFeature 
    {
        [SerializeField]
        int _Loop;
        public int Loop => _Loop;

        [SerializeField]
        int _Rank;
        public int Rank => _Rank;

        [SerializeField]
        string _EntityId;
        public string EntityId => _EntityId;

        [SerializeField]
        bool _Bounce;
        public bool Bounce => _Bounce;

        public Direction Direction =>
            Anchor ? Anchor.CubeFace : Direction.None;

        public override string ToString() =>
            $"<{Loop}:{Rank} {Coordinates}{(Bounce ? " (B)" : "")}>";

        bool MatchingDirection(Direction direction) =>
            Direction == direction || Direction == Direction.None;

        public void Configure(TiledCustomProperties props)
        {
            _Loop = props.Int(TiledConfiguration.instance.PathLoopKey, 0);
            _Rank = props.Int(TiledConfiguration.instance.RankKey);
            _Bounce = props.Bool(TiledConfiguration.instance.BounceKey, false);
            _EntityId = props.String(
                TiledConfiguration.instance.ObjEnemyIdKey,
                TiledConfiguration.instance.NPCClass);
        }

        public bool AtCheckpoint(GridEntity entity) =>
            EntityId == entity.Identifier && entity.Coordinates == Coordinates && MatchingDirection(entity.AnchorDirection);
        public bool AtCheckpoint(TDEnemy enemy) =>
            enemy.Id == EntityId && enemy.Entity.Coordinates == Coordinates && MatchingDirection(enemy.Entity.AnchorDirection);

        static List<TDPathCheckpoint> Checkpoints = new List<TDPathCheckpoint>();

        private void OnEnable()
        {
            Checkpoints.Add(this);
        }
        private void OnDisable()
        {
            Checkpoints.Remove(this);
        }
        private void OnDestroy()
        {
            Checkpoints.Remove(this);
        }

        public static IEnumerable<TDPathCheckpoint> GetAll(GridEntity entity) =>
             Checkpoints
                .Where(c => c.EntityId == entity.Identifier);

        public static IEnumerable<TDPathCheckpoint> GetAll(TDEnemy enemy) =>
            Checkpoints 
                .Where(c => c.EntityId == enemy.Id);

        public static IEnumerable<TDPathCheckpoint> GetAll(GridEntity entity, int rank) =>
            GetAll(entity, 0, rank);
        public static IEnumerable<TDPathCheckpoint> GetAll(GridEntity entity, int loop, int rank) =>
             Checkpoints
                .Where(c => c.EntityId == entity.Identifier && c.Loop == loop && c.Rank == rank);

        public static IEnumerable<TDPathCheckpoint> GetAll(TDEnemy enemy, int rank) =>
            GetAll(enemy, 0, rank);
        public static IEnumerable<TDPathCheckpoint> GetAll(TDEnemy enemy, int loop, int rank) =>
            Checkpoints 
                .Where(c => c.EntityId == enemy.Id && c.Loop == loop && c.Rank == rank);

        public static IEnumerable<TDPathCheckpoint> GetLoop(TDEnemy enemy, int loop) =>
            Checkpoints 
                .Where(c => c.EntityId == enemy.Id && c.Loop == loop);
    }
}
