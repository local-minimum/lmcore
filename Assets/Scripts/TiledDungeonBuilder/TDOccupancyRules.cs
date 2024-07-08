using LMCore.AbstractClasses;
using LMCore.Crawler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TDOccupancyRules : Singleton<TDOccupancyRules>, IOccupationRules
{
    public void HandleDeparture(GridEntity entity, IEnumerable<GridEntity> occupants)
    {
    }

    public void HandleMeeting(GridEntity entity, IEnumerable<GridEntity> occupants)
    {
    }

    public bool MayCoexist(GridEntity entity, IEnumerable<GridEntity> occupants)
    {
        if (entity.EntityType == GridEntityType.PlayerCharacter)
        {
            return !occupants.Any(o => o.EntityType == GridEntityType.Enemy || o.EntityType == GridEntityType.NPC);
        }

        if (entity.EntityType == GridEntityType.Enemy)
        {
            return !occupants.Any(o => o.EntityType == GridEntityType.PlayerCharacter || o.EntityType == GridEntityType.NPC);
        }

        if (entity.EntityType == GridEntityType.NPC)
        {
            return !occupants.Any(o => o.EntityType != GridEntityType.Critter);
        }

        return true;
    }
}
