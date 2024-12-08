using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Composites;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyHunting : TDEnemyBehaviour, IOnLoadSave
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

        string PrefixLogMessage(string message) =>
            $"Hunting {Enemy.name} ({target}): {message}";

        #region SaveState
        GridEntity target;
        List<KeyValuePair<Direction, Vector3Int>> previousPath;
        #endregion

        private void Awake()
        {
            enabled = false;
        }


        private void OnDisable()
        {
            target = null;
            previousPath = null;
        }

        public void InitHunt(GridEntity target)
        {
            if (target == null)
            {
                Debug.LogWarning(PrefixLogMessage("initing hunt without a target"));
            }
            this.target = target;
        }

        float nextCheck;

        private void Update()
        {
            /*
            Debug.Log(PrefixLogMessage($"Target({target == null}) " +
                $"nextCheck({Time.timeSinceLevelLoad < nextCheck}) " +
                $"moving({Enemy.Entity.Moving != Crawler.MovementType.Stationary}) " +
                $"falling({Enemy.Entity.Falling}"));
            */
            if (target == null) {
                Debug.LogError(PrefixLogMessage("Nothing to hunt"));
                Enemy.MayTaxStay = true;
                Enemy.UpdateActivity();
                return;
            }

            if (Time.timeSinceLevelLoad < nextCheck) return;

            var entity = Enemy.Entity;
            if (entity.Moving != Crawler.MovementType.Stationary) return;
            if (entity.Falling)
            {
                Enemy.Entity.MovementInterpreter.InvokeMovement(Movement.AbsDown, movementDuration * fallDurationFactor);
                nextCheck = Time.timeSinceLevelLoad + movementDuration * fallDurationFactor * 0.5f;
                return;
            }

            var dungeon = Enemy.Dungeon;
            if (dungeon.ClosestPath(entity, entity.Coordinates, target.Coordinates, maxPlayerSearchDepth, out var path))
            {
                Debug.Log(PrefixLogMessage("Found path"));
                if (previousPath != null && previousPath.Count > 0)
                {
                    if (previousPath[0].Value == entity.Coordinates)
                    {
                        previousPath = previousPath.Skip(1).ToList();
                    }
                }

                var pCount = previousPath?.Count ?? -100;
                if (Mathf.Abs(pCount - path.Count) < 2 && pCount > 0)
                {
                    // Debug.Log(PrefixLogMessage($"Resuing path:\n{string.Join(", ", previousPath)}\ninstead of {string.Join(", ", path)}"));
                    path = previousPath;
                } else
                {
                    // Debug.Log(PrefixLogMessage($"Found path: {string.Join(", ", path.Select(p => $"{p.Key} {p.Value}"))}"));
                    previousPath = path;
                }
                    

                var length = path.Count;
                if (length > 0)
                {
                    if (length > taxActivityStayDistance)
                    {
                        Enemy.MayTaxStay = true;
                    }

                    // Debug.Log(PrefixLogMessage(string.Join(", ", path)));
                    // TODO: Improve this logic
                    var (direction, coordinates) = path[0];
                    if (!NextActionCollidesWithPlayer(path) || direction != entity.LookDirection)
                    {
                        InvokePathBasedMovement(direction, coordinates, target.Coordinates, movementDuration, PrefixLogMessage);
                    }
                    nextCheck = Time.timeSinceLevelLoad + movementDuration * 0.5f;
                }

                if (length > checkActivityTransitionDistance)
                {
                    Enemy.UpdateActivity();
                }
            } else
            {
                if (previousPath != null && previousPath.Count > 0)
                {
                    if (previousPath[0].Value != target.Coordinates)
                    {

                        // If we repeatedly are following prevoius path we
                        // need to truncate it for each move we have made
                        if (previousPath[0].Value == entity.Coordinates)
                        {
                            previousPath = previousPath.Skip(1).ToList();
                        }

                        if (previousPath.Count > 0)
                        {
                            Debug.Log(PrefixLogMessage("Using previous path to player"));
                            // TODO: Improve this logic
                            var (direction, coordinates) = previousPath[0];
                            if (!NextActionCollidesWithPlayer(path) || direction != entity.LookDirection)
                            {
                                InvokePathBasedMovement(direction, coordinates, target.Coordinates, movementDuration, PrefixLogMessage);
                            }
                        }
                    }
                }
                Debug.LogWarning(PrefixLogMessage("Could not find path to player"));
                Enemy.MayTaxStay = true;
                nextCheck = Time.timeSinceLevelLoad + movementDuration;

                Enemy.UpdateActivity();
            }
        }

        #region Save/Load
        public EnemyHuntingSave Save() => 
            target != null ? new EnemyHuntingSave() {
                TargetId = target.Identifier,
                PreviousPathCoordinates = previousPath?.Select(kvp => kvp.Value)?.ToList(),
                PreviousPathDirections = previousPath?.Select(kvp => kvp.Key)?.ToList(),
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

            var huntingSave = lvlSave.enemies.FirstOrDefault(s => s.Id == Enemy.Id)?.hunting;
            if (huntingSave != null)
            {
                target = Dungeon.GetEntity(huntingSave.TargetId);
                previousPath = huntingSave.PreviousPathCoordinates
                    .Zip(
                        huntingSave.PreviousPathDirections,
                        (coords, dir) => new KeyValuePair<Direction, Vector3Int>(dir, coords))
                    .ToList();
            } else
            {
                target = null;
                previousPath = null;
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
