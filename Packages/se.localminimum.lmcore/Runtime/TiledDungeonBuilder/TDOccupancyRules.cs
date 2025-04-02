using LMCore.AbstractClasses;
using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDOccupancyRules : Singleton<TDOccupancyRules, TDOccupancyRules>, IOccupationRules
    {

        [SerializeField]
        bool PlayerMayJoinNPC;
        [SerializeField]
        bool AllowMultipleEnemiesOnTile;

        public void HandleDeparture(GridEntity entity, IEnumerable<GridEntity> occupants)
        {
        }

        public void HandleMeeting(GridEntity entity, IEnumerable<GridEntity> occupants)
        {
        }

        public bool MayCoexist(GridEntity entity, IEnumerable<GridEntity> occupants, out IEnumerable<GridEntity> conflicts)
        {
            var _conflicts = new List<GridEntity>();

            if (entity.EntityType == GridEntityType.PlayerCharacter)
            {
                _conflicts = occupants
                    .Where(o => o != entity && o.Alive && (o.EntityType == GridEntityType.Enemy || (!PlayerMayJoinNPC && o.EntityType == GridEntityType.NPC)))
                    .ToList();

            }

            if (entity.EntityType == GridEntityType.Enemy)
            {
                _conflicts = occupants
                    .Where(o => o != entity && o.Alive && (o.EntityType == GridEntityType.PlayerCharacter || o.EntityType == GridEntityType.NPC || (!AllowMultipleEnemiesOnTile && o.EntityType == GridEntityType.Enemy)))
                    .ToList();

            }

            if (entity.EntityType == GridEntityType.NPC)
            {
                _conflicts = occupants
                    .Where(o => o != entity && o.Alive && o.EntityType != GridEntityType.Critter)
                    .ToList();
            }

            conflicts = _conflicts;
            return _conflicts.Count == 0;
        }
    }
}
