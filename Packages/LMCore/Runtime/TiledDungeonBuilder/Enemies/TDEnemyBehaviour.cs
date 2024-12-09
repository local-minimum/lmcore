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

            /*
            if (entity.LookDirection == translationDirection)
            {
                // In most cases translation target is the same as a simple translation, but sometimes
                // we round corner and the like and then two checks are needed
                var simpleTranslationTarget = translationDirection.Translate(entity.Coordinates);
                if (simpleTranslationTarget == translationTarget && !Dungeon[translationTarget].AllowsEntryFrom(entity, translationDirection.Inverse())) { 
                    return;
                }

                if (translationTarget != simpleTranslationTarget)
                {
                    var secondaryTranslation = (translationTarget - simpleTranslationTarget).AsDirectionOrNone();

                    if (secondaryTranslation == Direction.None)
                    {
                        Debug.LogError(prefixLogMessage($"Translation {translationDirection} from {entity.Coordinates} to {translationTarget} " +
                            $"caused unexpected secondary translation of {translationTarget - simpleTranslationTarget}"));
                    } else
                    {
                        var options = new List<List<Direction>>()
                        {
                            new List<Direction>() { translationDirection, secondaryTranslation },
                            new List<Direction>() { secondaryTranslation, translationDirection },
                        };

                        if (!options.Any(translations => {
                            var coordinates = entity.Coordinates;
                            foreach (var direction in translations)
                            {
                                coordinates = direction.Translate(coordinates);
                                if (
                                    !(Dungeon[coordinates]?.AllowsEntryFrom(entity, direction.Inverse()) ?? false)
                                ) {
                                    return false;
                                }

                            }
                            return true;
                        })) {
                            return;
                        }
                    }
                }

                Debug.Log(prefixLogMessage("Moving forward"));
                entity.MovementInterpreter.InvokeMovement(IO.Movement.Forward, movementDuration);
            } else
            {
                var movement = translationDirection.AsMovement(entity.LookDirection, entity.Down);
                if (movement == IO.Movement.Up && entity.TransportationMode.HasFlag(TransportationMode.Flying))
                {
                    if (Dungeon[translationTarget].AllowsEntryFrom(entity, translationDirection.Inverse()))
                    {
                        // Flying up or climbing up
                        Debug.Log(prefixLogMessage("Moving up"));
                        entity.MovementInterpreter.InvokeMovement(movement, movementDuration);
                    }
                } else if (movement == IO.Movement.Down)
                {
                    if (Dungeon[translationTarget].AllowsEntryFrom(entity, translationDirection.Inverse()))
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
                    } else if (Dungeon[translationTarget].AllowsEntryFrom(entity, translationDirection.Inverse()))
                    {
                        // TODO: Consider better fallback / force getting off patrol
                        Debug.LogError(prefixLogMessage($"We have no movement based on needed direction {translationDirection} while looking {entity.LookDirection}"));
                        entity.MovementInterpreter.InvokeMovement(Movement.Forward, movementDuration);
                    }
                } 
            }
            */
        }
    }
}
