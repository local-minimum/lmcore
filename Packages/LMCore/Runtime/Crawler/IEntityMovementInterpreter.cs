﻿using System.Collections.Generic;

namespace LMCore.Crawler
{

    public delegate void EntityMovementEvent(GridEntity entity, MovementOutcome outcome, List<EntityState> states, float duration);

    public interface IEntityMovementInterpreter
    {
        public event EntityMovementEvent OnEntityMovement;
        public IDungeon Dungeon { set; }
    }
}
