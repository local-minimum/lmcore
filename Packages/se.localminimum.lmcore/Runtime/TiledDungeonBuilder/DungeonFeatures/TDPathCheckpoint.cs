using LMCore.Crawler;
using LMCore.EntitySM.State;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledImporter;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        [SerializeField]
        bool _Terminal;
        public bool Terminal => _Terminal;

        [SerializeField]
        StateType _ForcedState = StateType.None;
        public StateType ForceState => _ForcedState;

        [SerializeField]
        Direction _ForcedStateLookDirection = Direction.None;
        public Direction ForceStateLookDirection => _ForcedStateLookDirection;

        public Direction Direction =>
            Anchor ? Anchor.CubeFace : Direction.None;

        public override string ToString() =>
            $"<{Loop}:{Rank} {Coordinates}{(Bounce ? " (B)" : "")}{(Terminal ? " (T)" : "")}>";

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
            _Terminal = props.Bool(TiledConfiguration.instance.ObjTerminalKey, false);
            _ForcedState = StateTypeExtensions.From(props.String(TiledConfiguration.instance.ObjEnemyForceStateKey, "none"));
            _ForcedStateLookDirection = props.Direction(TiledConfiguration.instance.ObjLookDirectionKey, TDEnumDirection.None).AsDirection();

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

        public static IEnumerable<TDPathCheckpoint> GetAll(TDEnemy enemy, System.Func<TDPathCheckpoint, bool> predicate) =>
            Checkpoints
                .Where(c => c.EntityId == enemy.Id && predicate(c));

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
