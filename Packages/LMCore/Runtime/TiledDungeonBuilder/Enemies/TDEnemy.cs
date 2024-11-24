using LMCore.Crawler;
using LMCore.EntitySM;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    /// <summary>
    /// --- The Enemy ---
    /// 
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
        Personality Personality
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
        GridEntity Entity
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
        TiledDungeon Dungeon
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
        class EnemyPatrolPath
        {
            public int Loop;
            public int Rank;
            public bool Bounce;
            public Vector3Int Checkpoint;
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
        }
    }
}
