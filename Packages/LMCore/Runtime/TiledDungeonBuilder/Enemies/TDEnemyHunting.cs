using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyHunting : TDEnemyBehaviour, IOnLoadSave
    {
        [SerializeField]
        float movementDuration = 0.6f;

        [SerializeField]
        int looseAgressivityDistance = 5;

        string PrefixLogMessage(string message) =>
            $"Hunting {Enemy.name} ({target}): {message}";

        #region SaveState
        GridEntity target;
        #endregion

        private void OnDisable()
        {
            target = null;
        }

        public void InitHunt(GridEntity target)
        {
            if (target == null)
            {
                Debug.LogWarning(PrefixLogMessage("initing hunt without a target"));
            }
            this.target = target;
        }

        private void Update()
        {
            if (target == null) return;

            var entity = Enemy.Entity;
            if (entity.Moving != Crawler.MovementType.Stationary) return;

            var dungeon = Enemy.Dungeon;
            if (dungeon.ClosestPath(entity, entity.Coordinates, target.Coordinates, Enemy.ArbitraryMaxPathSearchDepth, out var path))
            {
                var length = path.Count;
                if (length > 0)
                {
                    if (length > looseAgressivityDistance)
                    {
                        Enemy.MayTaxStay = true;
                    }
                    var nextCoordinates = path[0];
                    InvokePathBasedMovement(nextCoordinates, movementDuration, PrefixLogMessage);
                }
            } else
            {
                Debug.LogWarning(PrefixLogMessage("Could not find path to player"));
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
