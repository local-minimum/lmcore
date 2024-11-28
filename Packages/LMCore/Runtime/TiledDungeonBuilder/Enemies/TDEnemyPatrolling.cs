using LMCore.Crawler;
using LMCore.TiledDungeon.Enemies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDEnemyPatrolling : MonoBehaviour
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

        float walkDuration = 1f;

        bool patrolling;
        TDEnemy.EnemyPatrolPath target;
        int direction;

        string PrefixLogMessage(string message) =>
            $"Patrolling {Enemy.name} ({patrolling}, {target}): {message}";


        public void SetCheckpointFromPatrolPath(TDEnemy.EnemyPatrolPath path)
        {
            target = path;
            patrolling = path != null;
            direction = 1;
            Debug.Log(PrefixLogMessage("Starting"));
        }

        private void Update()
        {
            if (!patrolling) return;
            var entity = Enemy.Entity;
            if (entity.Moving != Crawler.MovementType.Stationary) return;
            
            if (entity.Coordinates == target.Checkpoint)
            {
                GetNextCheckpoint();
            }

            var dungeon = Enemy.Dungeon;
            if (dungeon.ClosestPath(entity, entity.Coordinates, target.Checkpoint, 100, out var path))
            {
                if (path.Count > 0)
                {
                    var nextCoordinates = path[0];
                    if (entity.LookDirection.Translate(entity.Coordinates) == nextCoordinates)
                    {
                        entity.MovementInterpreter.InvokeMovement(IO.Movement.Forward, 1f);
                    } else
                    {
                        var offset = nextCoordinates - entity.Coordinates;
                        var wantedLook = offset.AsDirection();
                        var movement = wantedLook.AsMovement(entity.LookDirection, entity.Down);
                        entity.MovementInterpreter.InvokeMovement(movement, 0.5f);
                    }
                } else
                {
                    Debug.LogWarning(PrefixLogMessage("Didn't find a path to target"));
                }
            }
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
    }
}
