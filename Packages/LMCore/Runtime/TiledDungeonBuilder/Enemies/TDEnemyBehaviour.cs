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

        /// <summary>
        /// Cause relevant translation to approach a target
        /// </summary>
        /// <param name="translationDirection">Direction of next translation</param>
        /// <param name="translatedTarget">Expected coordinates after translation (accounts for rounding corners and such)</param>
        /// <param name="target">Reference point, i.e. player, to assist in turning direction</param>
        /// <param name="movementDuration">Duration of translations</param>
        /// <param name="prefixLogMessage">Formatter or log messages</param>
        protected void InvokePathBasedMovement(
            Direction translationDirection, 
            Vector3Int translatedTarget,
            Vector3Int target,
            float movementDuration,
            System.Func<string, string> prefixLogMessage)
        {
            var entity = Enemy.Entity;

            if (entity.LookDirection == translationDirection)
            {
                if (Dungeon[translatedTarget].AllowsEntryFrom(entity, translationDirection.Inverse())) { 
                    Debug.Log(prefixLogMessage("Moving forward"));
                    entity.MovementInterpreter.InvokeMovement(IO.Movement.Forward, movementDuration);
                }
            } else
            {
                var movement = translationDirection.AsMovement(entity.LookDirection, entity.Down);
                if (movement == IO.Movement.Up && entity.TransportationMode.HasFlag(TransportationMode.Flying))
                {
                    if (Dungeon[translatedTarget].AllowsEntryFrom(entity, translationDirection.Inverse()))
                    {
                        // Flying up or climbing up
                        Debug.Log(prefixLogMessage("Moving up"));
                        entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                    }
                } else if (movement == IO.Movement.Down)
                {
                    if (Dungeon[translatedTarget].AllowsEntryFrom(entity, translationDirection.Inverse()))
                    {
                        // Falling or flying down
                        Debug.Log(prefixLogMessage("Moving down"));
                        entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                    }
                } else
                {
                    Debug.Log(prefixLogMessage("Rotating"));
                    var yawBias = entity.LookDirection.AsPlanarRotation(entity.Down, entity.Coordinates, target);
                    movement = translationDirection.AsPlanarRotation(entity.LookDirection, entity.Down, yawBias);
                    if (movement != IO.Movement.None)
                    {
                        // We are turning
                        entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                    } else if (Dungeon[translatedTarget].AllowsEntryFrom(entity, translationDirection.Inverse()))
                    {
                        // TODO: Consider better fallback / force getting off patrol
                        Debug.LogError(prefixLogMessage($"We have no movement based on needed direction {translationDirection} while looking {entity.LookDirection}"));
                        entity.MovementInterpreter.InvokeMovement(Movement.Forward, movementDuration);
                    }
                } 
            }
        }
    }
}
