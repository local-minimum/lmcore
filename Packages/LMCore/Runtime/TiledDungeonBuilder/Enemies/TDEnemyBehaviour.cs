using LMCore.Crawler;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
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

        public bool FallsOnPlayer(List<KeyValuePair<Direction, Vector3Int>> path, out int index)
        {
            index = path.FindIndex(kvp => kvp.Value == Dungeon.Player.Coordinates);
            if (index < 0) return false;

            return path[Mathf.Max(index - 1, 0)].Key == Direction.Down;
        }

        public bool NextActionCollidesWithPlayer(List<KeyValuePair<Direction, Vector3Int>> path)
        {
            if (path == null) return false;

            var index = path.FindIndex(kvp => kvp.Value == Dungeon.Player.Coordinates);
            if (index < 0) return false;
            if (index == 0) return true;

            var translationsUntilPlayer = path
                .Take(index + 1)
                .Reverse()
                .SkipWhile(kvp => kvp.Key == Direction.Down)
                .Count();

            return translationsUntilPlayer == 1;
        }

        /// <summary>
        /// Cause relevant translation to approach a target
        /// </summary>
        /// <param name="translationDirection">Direction of next translation</param>
        /// <param name="target">Reference point, i.e. player, to assist in turning direction</param>
        /// <param name="movementDuration">Duration of translations</param>
        /// <param name="prefixLogMessage">Formatter or log messages</param>
        protected void InvokePathBasedMovement(
            Direction translationDirection, 
            Vector3Int target,
            float movementDuration,
            System.Func<string, string> prefixLogMessage)
        {
            var entity = Enemy.Entity;
            // var translationTarget = entity.Node.Neighbour(entity, translationDirection, out var targetAnchor);
            var outcome = entity.Node.AllowsTransition(entity, translationDirection, out var translationTarget, out var targetAnchor);
            if (outcome == MovementOutcome.Refused) return;

            if (translationDirection == entity.LookDirection)
            {
                Debug.Log(prefixLogMessage("Moving forward"));
                entity.MovementInterpreter.InvokeMovement(Movement.Forward, movementDuration);
                return;
            }

            var movement = translationDirection.AsMovement(entity.LookDirection, entity.Down);
            if (movement == Movement.Up || movement == Movement.Down)
            {
                Debug.Log(prefixLogMessage($"Moving {movement}"));
                entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                return;
            }

            var yawBias = entity.LookDirection.AsPlanarRotation(entity.Down, entity.Coordinates, target);
            movement = translationDirection.AsPlanarRotation(entity.LookDirection, entity.Down, yawBias);
            if (movement != Movement.None)
            {
                // We are turning
                entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
            } else if (Dungeon[translationTarget].AllowsEntryFrom(entity, translationDirection.Inverse()))
            {
                // TODO: Could we have a sane fallback here?
                Debug.LogError(prefixLogMessage($"We have no movement based on needed direction {translationDirection} while looking {entity.LookDirection}"));
                // entity.MovementInterpreter.InvokeMovement(Movement.Forward, movementDuration);
            }
        }
    }
}