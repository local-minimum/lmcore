using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledDungeon.SaveLoad;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDEnemyPatrolling : MonoBehaviour, IOnLoadSave
    {
        TDEnemy _enemy;
        TDEnemy Enemy { 
            get { 
                if (_enemy == null)
                {
                    _enemy = GetComponentInParent<TDEnemy>();
                }
                return _enemy; 
            } 
        }

        TiledDungeon _dungeon;
        protected TiledDungeon Dungeon {
            get {
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<TiledDungeon>();
                }
                return _dungeon;
            }
        }

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
            if (dungeon.ClosestPath(entity, entity.Coordinates, target.Coordinates, 100, out var path))
            {
                if (path.Count > 0)
                {
                    var nextCoordinates = path[0];
                    if (entity.LookDirection.Translate(entity.Coordinates) == nextCoordinates)
                    {
                        entity.MovementInterpreter.InvokeMovement(IO.Movement.Forward, movementDuration);
                    } else
                    {
                        var offset = nextCoordinates - entity.Coordinates;
                        var wantedLook = offset.AsDirection();
                        var movement = wantedLook.AsMovement(entity.LookDirection, entity.Down);
                        if (movement == IO.Movement.Up && entity.TransportationMode.HasFlag(TransportationMode.Flying))
                        {
                            // Flying up or climbing up
                            entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                        } else if (movement == IO.Movement.Down)
                        {
                            // Falling or flying down
                            entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                        } else
                        {
                            movement = wantedLook.AsPlanarRotation(entity.LookDirection, entity.Down);
                            if (movement != IO.Movement.None)
                            {
                                // We are turning
                                entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                            } else
                            {
                                // TODO: Consider better fallback / force getting off patrol
                                Debug.LogError(PrefixLogMessage($"We ave no movement based on needed direction {wantedLook} while looking {entity.LookDirection}"));
                                entity.MovementInterpreter.InvokeMovement(Movement.Forward, movementDuration);
                            }
                        } 
                    }
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
    }
}
