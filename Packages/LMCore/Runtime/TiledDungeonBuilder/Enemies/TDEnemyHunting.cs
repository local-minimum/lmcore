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

        [SerializeField]
        int taxActivityStayDistance = 5;

        [SerializeField]
        int checkActivityTransitionDistance = 3;

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

        private void OnEnable()
        {
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
            if (target == null) {
                Debug.LogError(PrefixLogMessage("Nothing to hunt"));
                Enemy.MayTaxStay = true;
                Enemy.UpdateActivity();
                return;
            }
            if (Time.timeSinceLevelLoad < nextCheck) return;

            var entity = Enemy.Entity;
            if (entity.Moving != Crawler.MovementType.Stationary) return;

            var dungeon = Enemy.Dungeon;
            if (dungeon.ClosestPath(entity, entity.Coordinates, target.Coordinates, Enemy.ArbitraryMaxPathSearchDepth, out var path))
            {
                Debug.Log(PrefixLogMessage($"Found path: {string.Join(", ", path.Select(p => $"{p.Key} {p.Value}"))}"));
                previousPath = path;
                var length = path.Count;
                if (length > 0)
                {
                    if (length > taxActivityStayDistance)
                    {
                        Enemy.MayTaxStay = true;
                    }
                    var (direction, coordinates) = path[0];
                    InvokePathBasedMovement(direction, coordinates, movementDuration, PrefixLogMessage);
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
                            var (direction, coordinates) = previousPath[0];
                            InvokePathBasedMovement(direction, coordinates, movementDuration, PrefixLogMessage);
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
            } else
            {
                target = null;
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
