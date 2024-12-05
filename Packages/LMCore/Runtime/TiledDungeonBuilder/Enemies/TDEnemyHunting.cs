using LMCore.Crawler;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyHunting : TDEnemyBehaviour 
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
    }
}
