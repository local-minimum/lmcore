using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledDungeon.SaveLoad;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDEnemyPatrolling : TDEnemyBehaviour, IOnLoadSave
    {

        [SerializeField, Tooltip("Note that the turn durations are scaled by Entity abilities")]
        float movementDuration = 2f;

        #region SaveState
        bool Patrolling { get; set; }
        TDPathCheckpoint target;
        int direction;
        #endregion
        
        public bool HasTarget => target != null;

        string PrefixLogMessage(string message) =>
            $"Patrolling {Enemy.name} ({Patrolling}, {target}): {message}";


        public void SetCheckpointFromPatrolPath(TDPathCheckpoint path)
        {
            target = path;
            Patrolling = path != null;
            direction = 1;
            Debug.Log(PrefixLogMessage("Starting"));
        }

        private void OnEnable()
        {
            Patrolling = target != null;
        }

        private void OnDisable()
        {
            Patrolling = false;
        }

        private void Update()
        {
            if (!Patrolling) return;
            var entity = Enemy.Entity;
            if (entity.Moving != Crawler.MovementType.Stationary) return;
            
            if (entity.Coordinates == target.Coordinates)
            {
                GetNextCheckpoint();
                Enemy.UpdateActivity();
                if (!Patrolling) return;
            }

            var dungeon = Enemy.Dungeon;
            if (dungeon.ClosestPath(entity, entity.Coordinates, target.Coordinates, Enemy.ArbitraryMaxPathSearchDepth, out var path))
            {
                if (path.Count > 0)
                {
                    var (direction, _) = path[0];
                    InvokePathBasedMovement(direction, movementDuration, PrefixLogMessage);
                } else
                {
                    // TODO: Consider better fallback / force getting off patroll
                    Debug.LogWarning(PrefixLogMessage("Didn't find a path to target"));
                    entity.MovementInterpreter.InvokeMovement(Movement.Forward, movementDuration);
                }
            }

            Enemy.MayTaxStay = true;
        }

        void GetNextCheckpoint()
        {
            Debug.Log(PrefixLogMessage("Getting next checkpoint"));
            var options = Enemy.GetNextCheckpoints(target, direction, out int newDirection);
            // TODO: Handle not finding anything
            // TODO: Handle selecting which checkpoint better
            if (options != null)
            {
                direction = newDirection;
                target = options.First();
            }
        }

        public EnemyPatrollingSave Save() =>
            Patrolling ? 
                new EnemyPatrollingSave() { 
                    active = Patrolling,
                    direction = direction,
                    loop = target?.Loop ?? 0,
                    rank = target?.Rank ?? 0,
                } : 
                null;

        #region Save/Load
        public int OnLoadPriority => 500;
        private void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }

            var lvl = Dungeon.MapName;

            var lvlSave = save.levels[lvl];
            if (lvlSave == null)
            {
                return;
            }

            var patrollingSave = lvlSave.enemies.FirstOrDefault(s => s.Id == Enemy.Id)?.patrolling;
            if (patrollingSave != null)
            {
                target = TDPathCheckpoint.GetAll(Enemy, patrollingSave.loop, patrollingSave.rank).FirstOrDefault();
                if (target == null)
                {
                    Debug.LogError(PrefixLogMessage($"Could not find target (loop {patrollingSave.loop}, rank {patrollingSave.rank})"));
                    Patrolling = false; 
                }
                else
                {
                    Patrolling = patrollingSave.active;
                }
                direction = patrollingSave.direction;
            }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
        #endregion
    }
}
