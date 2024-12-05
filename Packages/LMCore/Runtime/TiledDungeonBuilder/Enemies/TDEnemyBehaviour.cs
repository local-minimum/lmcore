using LMCore.Crawler;
using LMCore.IO;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public abstract class TDEnemyBehaviour : MonoBehaviour
    {
        TDEnemy _enemy;
        protected TDEnemy Enemy { 
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

        protected void InvokePathBasedMovement(
            Vector3Int nextTarget, 
            float movementDuration,
            System.Func<string, string> prefixLogMessage)
        {
            var entity = Enemy.Entity;

            if (entity.LookDirection.Translate(entity.Coordinates) == nextTarget)
            {
                Debug.Log(prefixLogMessage("Moving forward"));
                entity.MovementInterpreter.InvokeMovement(IO.Movement.Forward, movementDuration);
            } else
            {
                var offset = nextTarget - entity.Coordinates;
                var wantedLook = offset.AsDirection();
                var movement = wantedLook.AsMovement(entity.LookDirection, entity.Down);
                if (movement == IO.Movement.Up && entity.TransportationMode.HasFlag(TransportationMode.Flying))
                {
                    // Flying up or climbing up
                    Debug.Log(prefixLogMessage("Moving up"));
                    entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                } else if (movement == IO.Movement.Down)
                {
                    // Falling or flying down
                    Debug.Log(prefixLogMessage("Moving down"));
                    entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                } else
                {
                    Debug.Log(prefixLogMessage("Rotating"));
                    movement = wantedLook.AsPlanarRotation(entity.LookDirection, entity.Down);
                    if (movement != IO.Movement.None)
                    {
                        // We are turning
                        entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                    } else
                    {
                        // TODO: Consider better fallback / force getting off patrol
                        Debug.LogError(prefixLogMessage($"We have no movement based on needed direction {wantedLook} while looking {entity.LookDirection}"));
                        entity.MovementInterpreter.InvokeMovement(Movement.Forward, movementDuration);
                    }
                } 
            }
        }
    }
}
