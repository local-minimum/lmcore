using LMCore.Crawler;
using LMCore.EntitySM.State.Critera;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyHunting : TDAbsEnemyBehaviour, IOnLoadSave
    {
        [SerializeField]
        float movementDuration = 0.6f;

        [SerializeField, Range(0f, 1f)]
        float fallDurationFactor = 0.5f;

        [SerializeField]
        int taxActivityStayDistance = 5;

        [SerializeField]
        int checkActivityTransitionDistance = 3;

        [SerializeField]
        int maxPlayerSearchDepth = 7;

        [SerializeField]
        CustomCriteria CheckFightCriteria;

        [SerializeField]
        RepititionCriteria FailedHuntCriteria;

        [SerializeField]
        bool strafeWhenClose = true;

        [SerializeField, Range(0, 1f)]
        float flankProbability = 0.3f;

        string PrefixLogMessage(string message) =>
            $"Hunting {Enemy.name} @ {Enemy.Entity.Coordinates} Target({target}): {message}";

        public override string ToString() => $"<Hunting Enabled({enabled}) Target({target})>";

        #region SaveState
        GridEntity target;
        // Note: that `previousPath` also is in save state
        #endregion

        private void OnEnable()
        {
            Enemy.OnResumeEnemy += Enemy_OnResumeEnemy;
        }

        private void OnDisable()
        {
            // We want to keep previous path in some scenarios
            // E.g. we've gotten into fighting range, if player moves out we could use
            // prevoius path when getting back to hunting

            Enemy.OnResumeEnemy -= Enemy_OnResumeEnemy;
        }

        public void InitHunt(GridEntity target)
        {
            if (FailedHuntCriteria != null) FailedHuntCriteria.Clear();

            if (target == null)
            {
                Debug.LogWarning(PrefixLogMessage("initing hunt without a target"));
                previousPath = null;
            }
            else if (target != this.target)
            {
                previousPath = null;
            }
            else if (previousPath != null)
            {
                var myPosition = previousPath
                    .FindIndex(p => p.Checkpoint.Coordinates == Enemy.Entity.Coordinates);

                if (myPosition < 0 || myPosition == previousPath.Count - 1)
                {
                    // We are not on the previous path, lets forget about it
                    // or we have completed it so it also makes no sense
                    previousPath = null;
                }
                else if (myPosition > 0)
                {
                    previousPath = previousPath.Skip(myPosition).ToList();
                }
            }

            this.target = target;
        }

        float nextCheck;


        private void Enemy_OnResumeEnemy(float pauseDuration)
        {
            nextCheck += pauseDuration;
        }

        /// <summary>
        /// If there's a reasonable movement for the entity then queue it up.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="path">Path towards target, must be at least 1 item long</param>
        bool DecideOnMovement(GridEntity entity, List<PathTranslation> path)
        {
            int count = path.Count;
            bool weAreOnFirst = count >= 1 && path[0].Checkpoint.Coordinates == entity.Coordinates;

            if (count <= 1 || !weAreOnFirst) return false;

            // Debug.Log(PrefixLogMessage(string.Join(", ", path)));

            // Considering the next position we should get to
            var direction = path[1].TranslationHere;
            var refuseTranslation = RefuseTranslationByHistory(path);

            if (refuseTranslation && entity.LookDirection == direction) return false;

            var doStrafe = !refuseTranslation
                && strafeWhenClose
                // We are within allowed strafing distance
                // This assumes player is still on the last position
                && path.Count == 3
                // We are not looking in the direction of the next step
                && path[1].TranslationHere != entity.LookDirection
                // The last translation must be in the look direction
                && path[2].TranslationHere == entity.LookDirection;

            if (!NextActionCollidesWithPlayer(path, entity.LookDirection, doStrafe))
            {
                var movement = InvokePathBasedMovement(
                    direction,
                    target.Coordinates,
                    movementDuration,
                    doStrafe,
                    PrefixLogMessage);

                Debug.Log(PrefixLogMessage($"Got movement: {movement}"));
                if (Enemy.animator != null)
                {
                    if (movement.IsTranslation() && !string.IsNullOrEmpty(translateAnimTrigger))
                    {
                        Enemy.animator.SetTrigger(translateAnimTrigger);
                    }
                    else if (movement.IsRotation() && !string.IsNullOrEmpty(rotateAnimTrigger))
                    {
                        Enemy.animator.SetTrigger(rotateAnimTrigger);
                    }
                    else if (movement == Movement.None && !string.IsNullOrEmpty(noMovementAnimTrigger))
                    {
                        Debug.Log(PrefixLogMessage($"Doing the no movement animation: {noMovementAnimTrigger}"));
                        Enemy.animator.SetTrigger(noMovementAnimTrigger);
                    }
                }

                return true;
            }

            return false;
        }

        public bool RefuseTranslationByHistory(List<PathTranslation> path)
        {
            // If there's nowhere to go there's no translation to refuse
            if (path.Count < 2) return false;

            var target = path[1].Checkpoint.Coordinates;
            var n = historicPositions.Count(p => p == target);
            if (n >= refuseTranslationRepetitionThreshold)
            {
                return Random.value < refuseTranslationProbability ? true : false;
            }

            return false;
        }

        [SerializeField]
        int refuseTranslationRepetitionThreshold = 4;

        [SerializeField]
        float refuseTranslationProbability = 0.4f;

        [SerializeField]
        int maxPositionHistory = 20;

        [SerializeField]
        float preferReusePreviousPathProbability = 0.8f;

        Queue<Vector3Int> historicPositions = new Queue<Vector3Int>();
        Vector3Int mostRecentPositionRecorded;

        void TruncateHistory()
        {
            var n = historicPositions.Count;
            for (int i = 0, l = Mathf.Max(n - maxPositionHistory, 0); i < l; i++)
            {
                historicPositions.Dequeue();
            }
        }

        void ExtendHistory(GridEntity entity)
        {
            if (entity.Coordinates == mostRecentPositionRecorded) return;

            mostRecentPositionRecorded = entity.Coordinates;
            historicPositions.Enqueue(mostRecentPositionRecorded);
            TruncateHistory();
        }

        /// <summary>
        /// Removes first item in previous path if it seems we executed it
        /// </summary>
        /// <returns>If the previous path got truncated</returns>
        bool TruncatePreviousPath()
        {
            // Truncate previous path one step if we moved
            if (previousPath == null)
            {
                return false;
            }

            var n = previousPath.Count;

            for (int i = 0; i < n; i++)
            {

                if (previousPath[i].Checkpoint.IsHere(Enemy.Entity))
                {
                    if (i > 0)
                    {
                        var newPath = previousPath.Skip(i).ToList();
                        if (newPath.Count > 1)
                        {
                            previousPath = newPath;
                            return true;
                        }

                        // We've exhausted the path
                        previousPath = null;
                        return false;
                    }
                    return false;
                }
            }

            Debug.LogWarning(PrefixLogMessage(
                $"Didn't follow planned path, thought I'd be at second checkpoint of {previousPath.Debug()}, but am at {Enemy.Entity.Coordinates}"));
            previousPath = null;
            return false;
        }

        List<PathTranslation> ChooseNewOrPreviousPath(List<PathTranslation> newPath)
        {
            if (newPath == null) return previousPath;

            // Check if we should reuse previous path
            var pCount = previousPath?.Count ?? -100;
            if (pCount <= 0)
            {
                previousPath = newPath;
                return newPath;
            }

            var lookDirection = Enemy.Entity.LookDirection;
            var costDelta = previousPath.Cost(lookDirection) - newPath.Cost(lookDirection);
            if (costDelta <= 0 || (Mathf.Abs(costDelta) < 2 && (attemptingFlank || Random.value < preferReusePreviousPathProbability)))
            {
                Debug.Log(PrefixLogMessage($"Resuing previous path:\n{string.Join(", ", previousPath)}\ninstead of {string.Join(", ", newPath)}"));
                return previousPath;
            }

            // Debug.Log(PrefixLogMessage($"Found path: {string.Join(", ", path.Select(p => $"{p.Key} {p.Value}"))}"));
            previousPath = newPath;
            return newPath;
        }

        bool ExecutePath(List<PathTranslation> path)
        {
            var length = path.Count;
            var didMove = false;
            if (length > 0)
            {
                if (length > taxActivityStayDistance)
                {
                    Enemy.MayTaxStay = true;
                }

                didMove = DecideOnMovement(Enemy.Entity, path);

                nextCheck = Time.timeSinceLevelLoad + (movementDuration * 0.5f);
            }

            return didMove;
        }

        void CheckUpdateEnemyActivity(List<PathTranslation> path, bool force = false)
        {
            if (force || path.Count > checkActivityTransitionDistance || CheckFightCriteria.Permissable(Enemy.Personality, out var _))
            {
                Enemy.UpdateActivity();
            }
        }

        List<PathTranslation> GetShortestFlankPath()
        {
            var entity = Enemy.Entity;

            var flankPath = new List<Vector3Int> {
                target.Project(Movement.StrafeLeft, out var _, out var _),
                target.Project(Movement.StrafeRight, out var _, out var _),
                target.Project(Movement.Backward, out var _, out var _),
            }
                .Where(o => Dungeon.HasNodeAt(o))
                .Select(o =>
                {
                    if (Dungeon.ClosestPath(entity, entity.Coordinates, o, maxPlayerSearchDepth, out var newPath, refuseSafeZones: true))
                    {
                        return newPath;
                    }
                    return null;
                })
                .Where(p => p != null)
                .OrderBy(p => p.Cost(entity.LookDirection))
                .FirstOrDefault();

            if (flankPath != null && flankPath.Count > 0)
            {
                flankPath.Extend(target);
            }

            return flankPath;
        }

        bool attemptingFlank;

        private void Update()
        {
            if (Paused) return;
            /*
            Debug.Log(PrefixLogMessage($"Target({target == null}) " +
                $"nextCheck({Time.timeSinceLevelLoad < nextCheck}) " +
                $"moving({Enemy.Entity.Moving != Crawler.MovementType.Stationary}) " +
                $"falling({Enemy.Entity.Falling}"));
            */
            if (target == null)
            {
                Debug.LogError(PrefixLogMessage("Nothing to hunt"));
                if (FailedHuntCriteria != null) FailedHuntCriteria.Increase();
                Enemy.MayTaxStay = true;
                Enemy.UpdateActivity();
                return;
            }

            if (Time.timeSinceLevelLoad < nextCheck) return;

            var entity = Enemy.Entity;
            ExtendHistory(entity);

            if (entity.Moving != MovementType.Stationary) return;
            if (entity.Falling)
            {
                Enemy.Entity.MovementInterpreter.InvokeMovement(
                    Movement.AbsDown,
                    movementDuration * fallDurationFactor,
                    true);
                nextCheck = Time.timeSinceLevelLoad + (movementDuration * fallDurationFactor * 0.5f);
                return;
            }

            var dungeon = Enemy.Dungeon;

            TruncatePreviousPath();

            // Attempt a flank move but lets not change our mind too often
            // about what flank
            if (!attemptingFlank && Random.value < flankProbability)
            {
                var flankPath = GetShortestFlankPath();

                if (flankPath != null && flankPath.Count > 0)
                {
                    if (ExecutePath(flankPath))
                    {
                        CheckUpdateEnemyActivity(flankPath);
                        previousPath = flankPath;
                        attemptingFlank = true;
                        return;
                    }
                }
            }

            // Use closest path to player
            if (dungeon.ClosestPath(entity, entity.Coordinates, target.Coordinates, maxPlayerSearchDepth, out var newPath, refuseSafeZones: true))
            {
                // Debug.Log(PrefixLogMessage($"Found path: {string.Join(", ", path)}"));

                var chosenPath = ChooseNewOrPreviousPath(newPath);
                var executed = ExecutePath(chosenPath);
                CheckUpdateEnemyActivity(chosenPath, !executed);

                if (executed)
                {
                    // We could actually be using previous path, which could be a flanking path
                    if (chosenPath == newPath)
                    {
                        attemptingFlank = false;
                    }
                    return;
                }
            }

            // We found no path lets work with our previous path
            if (previousPath != null && previousPath.Count > 0)
            {
                Debug.Log(PrefixLogMessage("Using previous path to player"));
                if (!DecideOnMovement(entity, previousPath))
                {
                    Debug.LogWarning(PrefixLogMessage("Could not find path to player"));
                    attemptingFlank = false;
                }
                else
                {
                    // Still working on previous hypothesis
                    return;
                }
            }
            else
            {
                // We have no path, i.e we are not flanking
                attemptingFlank = false;
            }

            if (target != null && entity.LookDirection.InDirection(target.Coordinates - entity.Coordinates))
            {
                var targetCoords = entity.LookDirection.Translate(entity.Coordinates);
                if (dungeon.ClosestPath(entity, entity.Coordinates, targetCoords, 3, out var fallbackPath, refuseSafeZones: true))
                {
                    if (DecideOnMovement(entity, fallbackPath))
                    {
                        return;
                    }
                }
            }

            if (FailedHuntCriteria != null) FailedHuntCriteria.Increase();
            Enemy.MayTaxStay = true;
            nextCheck = Time.timeSinceLevelLoad + movementDuration;

            Enemy.UpdateActivity();
        }

        [ContextMenu("Info")]
        void Info()
        {
            if (target == null)
            {
                Debug.Log(PrefixLogMessage("No target"));
            }
            else
            {
                var entity = Enemy.Entity;
                var findsPath = Dungeon.ClosestPath(entity, entity.Coordinates, target.Coordinates, maxPlayerSearchDepth, out var path, refuseSafeZones: true);
                Debug.Log(PrefixLogMessage($"Previous path: {previousPath?.Debug() ?? "[Null]"}\n" +
                    $"Finds path({findsPath}): {path?.Debug() ?? "[Null]"}"));
            }
        }

        #region Save/Load
        public EnemyHuntingSave Save() =>
            target != null ? new EnemyHuntingSave()
            {
                TargetId = target.Identifier,
                PreviousPath = previousPath,
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

            var huntingSave = lvlSave.enemies.FirstOrDefault(s => s.Id == Enemy.Id && s.Alive)?.hunting;
            if (huntingSave != null)
            {
                target = Dungeon.GetEntity(huntingSave.TargetId);
                previousPath = huntingSave.PreviousPath;
            }
            else
            {
                target = null;
                previousPath = null;
                enabled = false;
                Debug.Log(PrefixLogMessage("Disabling hunting on load"));
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
