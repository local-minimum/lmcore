using LMCore.Crawler;
using LMCore.EntitySM;
using LMCore.EntitySM.State;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    /// <summary>
    /// Coordinating script for each enemy.
    /// 
    /// Various Tiled configurations that may interact with an enemy:
    /// 
    /// --- The Enemy ---
    /// Class/Type: TiledConfiguration.EnemyClass
    /// Custom Properties:
    /// * TiledConfigration.ObjEnemyClassIdKey (string): The type of enemy to spawn in. 
    /// * TiledConfigration.ObjEnemyIdKey (string): The unique id for the enemy on the level 
    /// 
    /// --- Its Home Area ---
    /// Place that enemy returns to when not interacting with the player.
    /// 
    /// Class/Type: TiledConfiguration.ObjHomeAreaKey
    /// Custom Properties:
    /// * TiledConfigration.ObjEnemyIdKey (string): Must match the id of the enemy, not the
    /// class id.
    /// 
    /// --- Its Paths ---
    /// Enemy paths are points or areas (checkpoints) that the enemy pass through in sequence.
    /// 
    /// Class/Type: TiledConfiguration.ObjPathKey
    /// Curstom Properties:
    /// * TiledConfigration.ObjEnemyIdKey (string): Must match the id of the enemy, not the
    /// class id.
    /// * TiledConfiguration.LoopKey (int): If more than one patrol path is 
    /// available to the enemy, it needs to be included to identify the different paths
    /// * TiledConfiguration.RankKey (int): Required id for the order of the checkpoints in
    /// the path. If multiple has same rank, there's a checkpoint area, the closest one is
    /// used.
    /// * TiledConfiguration.BounceKey (bool): If upon reaching this checkpoint, the path
    /// should be inverted. Typically for non looping paths, both ends should have this set.
    /// </summary>
    public class TDEnemy : MonoBehaviour
    {
        [SerializeField, Tooltip("Identifier when instancing, not to confuse with identifier for defining individual's areas and checkpoints")]
        string _classId = System.Guid.NewGuid().ToString();
        public string ClassId => _classId;

        [ContextMenu("Generate Class ID")]
        void GenerateClassID()
        {
            _classId = System.Guid.NewGuid().ToString();
        }

        #region Lazy Refs
        ActivityManager _ActivityManager;
        ActivityManager ActivityManager
        {
            get { 
                if (_ActivityManager == null)
                {
                    _ActivityManager = GetComponentInChildren<ActivityManager>();
                }
                return _ActivityManager; 
            }
        }

        Personality _personality;
        public Personality Personality
        {
            get { 
                if (_personality == null)
                {
                    _personality = GetComponentInChildren<Personality>();
                }
                return _personality; 
            }
        }

        GridEntity _Entity;
        public GridEntity Entity
        {
            get
            {
                if (_Entity == null)
                {
                    _Entity = GetComponent<GridEntity>();
                }

                return _Entity;
            }

        }

        TiledDungeon _TiledDungeon;
        public TiledDungeon Dungeon
        {
            get
            {
                if (_TiledDungeon == null)
                {
                    _TiledDungeon = GetComponentInParent<TiledDungeon>();
                }

                return _TiledDungeon;
            }
        }
        #endregion

        protected string PrefixLogMessage(string message) =>
            $"Enemy {ClassId} - {id}: {message}";

        [SerializeField, HideInInspector]
        string id;

        bool MyHomeAreasFilter(Vector3Int position, TDNodeConfig config)
        {
            return config.HasObject(
                TiledConfiguration.InstanceOrCreate().ObjHomeAreaKey,
                props => props.String(TiledConfiguration.instance.ObjEnemyIdKey) == id);
        }

        bool MyPathFilter(Vector3Int position, TDNodeConfig config)
        {
            return config.HasObject(
                TiledConfiguration.InstanceOrCreate().ObjPathKey,
                props => props.String(TiledConfiguration.instance.ObjEnemyIdKey) == id);
        }

        Vector3 CoordinatesExtractor(Vector3Int position, TDNodeConfig config) => position;

        [System.Serializable]
        public class EnemyPatrolPath
        {
            public int Loop;
            public int Rank;
            public bool Bounce;
            public Vector3Int Checkpoint;

            public override string ToString() =>
                $"<{Loop}:{Rank} {Checkpoint}{(Bounce ? " (B)" : "")}>";
        }

        IEnumerable<EnemyPatrolPath> EnemyPathExtractor(Vector3Int position, TDNodeConfig config) =>
            config.SelectWhen(
                TiledConfiguration.InstanceOrCreate().ObjPathKey,
                props => props.String(TiledConfiguration.instance.ObjEnemyIdKey) == id,
                props => new EnemyPatrolPath()
                {
                    Loop = props.Int(TiledConfiguration.instance.PathLoopKey, 0),
                    Rank = props.Int(TiledConfiguration.instance.RankKey),
                    Bounce = props.Bool(TiledConfiguration.instance.BounceKey, false),
                    Checkpoint = position,
                }
            );

        [SerializeField, HideInInspector]
        List<List<EnemyPatrolPath>> PatrolPaths = new ();

        [SerializeField, HideInInspector]
        List<Vector3> HomeArea = new List<Vector3>();

        [ContextMenu("Info")]
        void Info()
        {
            var paths = PatrolPaths.Count() == 0 ? "None" : string.Join(" | ", PatrolPaths.Select(loop => string.Join(", ", loop)));
            var areas = HomeArea.Count() == 0 ? "None" : string.Join(", ", HomeArea);

            Debug.Log(PrefixLogMessage($"Home area: {areas} / Paths {paths}"));
        }

        [ContextMenu("Reconfigure")]
        void Reconfigure()
        {
            Configure(id);
        }


        public void Configure(string id)
        {
            this.id = id;
            var dungeon = Dungeon;

            if (dungeon == null)
            {
                Debug.LogError(PrefixLogMessage("Can't configure an enemy that is not in a dungeon"));
                return;
            }

            HomeArea = dungeon
                .GetFromConfigs(MyHomeAreasFilter, CoordinatesExtractor)
                .ToList();

            PatrolPaths = dungeon
                .GetFromConfigs(MyPathFilter, EnemyPathExtractor)
                .SelectMany(p => p)
                .OrderBy(p => p.Rank)
                .GroupBy(p => p.Loop)
                .Select(g => g.ToList())
                .ToList();

            Info();
        }

        private void OnEnable()
        {
            ActivityState.OnStayState += ActivityState_OnStayState;
            ActivityState.OnEnterState += ActivityState_OnEnterState;
        }

        private void OnDisable()
        {
            ActivityState.OnStayState -= ActivityState_OnStayState;
            ActivityState.OnEnterState -= ActivityState_OnEnterState;
        }

        bool mayTaxStay;
        ActivityState activeState;

        private void ActivityState_OnStayState(ActivityManager manager, ActivityState state)
        {
            if (manager != ActivityManager || !mayTaxStay) return;

            activeState = state;
            state.TaxStayPersonality(Personality);
            mayTaxStay = false;
        }

        private void Update()
        {
            if (activeState == null)
            {
                Debug.Log(PrefixLogMessage("Updating activity"));
                UpdateActivity();
            }
        }

        /// <summary>
        /// Call this whenever the enemy has done something or the player
        /// </summary>
        void UpdateActivity()
        {
            mayTaxStay = true;
            ActivityManager.CheckTransition();
        }

        private void ActivityState_OnEnterState(ActivityManager manager, ActivityState state)
        {
            if (manager != ActivityManager) return;

            switch (state.State)
            {
                case StateType.Patrolling:
                    SetPatrolGoal();
                    break;
                case StateType.Guarding:
                    SetGuardBehavior();
                    break;
            }
        }

        EnemyPatrolPath ClosestCheckpoint(int loop = -1)
        {
            var entity = Entity;
            var dungeon = Dungeon;
            EnemyPatrolPath closest = null;
            int closestDistance = int.MaxValue;

            for (int i = 0, l = PatrolPaths.Count; i < l; i++)
            {
                var loopPath = PatrolPaths[i];
                for (int j = 0, m = loopPath.Count; j < m; j++)
                {
                    var path = loopPath[j];
                    if (path.Loop == loop || loop < 0)
                    {
                        if (entity.Coordinates.ManhattanDistance(path.Checkpoint) >= closestDistance)
                        {
                            continue;
                        }

                        if (dungeon.ClosestPath(
                            entity,
                            entity.Coordinates,
                            path.Checkpoint,
                            closestDistance,
                            out var currentPath))
                        {
                            if (currentPath.Count() < closestDistance)
                            {
                                closest = path;
                                closestDistance = currentPath.Count();
                            }
                        }
                    }
                }
            }

            return closest;
        }

        public IEnumerable<EnemyPatrolPath> GetCheckpoints(int loop, int rank)
        {
            for (int i = 0, l = PatrolPaths.Count; i < l; i++)
            {
                var loopPath = PatrolPaths[i];
                for (int j = 0, m = loopPath.Count; j < m; j++)
                {
                    var path = loopPath[j];
                    if (path.Loop == loop && path.Rank == rank) yield return path;
                }
            }
        }

        int LoopMaxRank(int loop) =>
            PatrolPaths.Max(loopPath => loopPath.Max(path => path.Rank));

        public IEnumerable<EnemyPatrolPath> GetNextCheckpoints(
            EnemyPatrolPath current, 
            int direction,
            out int newDirection)
        {
            if (current.Bounce)
            {
                direction *= -1;
            }
            var nextRank = current.Rank + direction;

            var loopMax = LoopMaxRank(current.Loop);
            if (nextRank < 0)
            {
                nextRank = loopMax;
            }
            else if (nextRank > loopMax)
            {
                nextRank = 0;
            }

            newDirection = direction;
            return GetCheckpoints(current.Loop, nextRank);
        }

        void SetPatrolGoal()
        {
            var patrol = GetComponentInChildren<TDEnemyPatrolling>(true);
            if (patrol == null)
            {
                Debug.LogError(PrefixLogMessage("I don't have a patrolling pattern"));
                return;
            }

            var  pathCheckpoint = ClosestCheckpoint();
            if (pathCheckpoint == null)
            {
                Debug.LogError(PrefixLogMessage("There's no closest checkpoint for me"));
                Info();
            } else
            {
                patrol.SetCheckpointFromPatrolPath(pathCheckpoint);
            }
        }

        void SetGuardBehavior()
        {
        }
    }
}
