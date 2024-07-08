using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public enum MovementOutcome
    {
        /// <summary>
        /// It is completely impossible, can't even attempt ed
        /// </summary>
        Refused,

        /// <summary>
        /// Typically you can reach end of tile but no more
        /// </summary>
        Blocked,

        /// <summary>
        /// Typically going from the floor center to wall center or so
        /// </summary>
        NodeInternal,

        /// <summary>
        /// Movement continues onto another node on the same surface
        /// </summary>
        NodeExit
    }

    public interface IDungeonNode
    {
        public MovementOutcome AllowsMovement(GridEntity entity, Direction anchor, Direction direction);
        public bool AllowsEntryFrom(GridEntity entity, Direction direction);
        public bool AllowsRotating(GridEntity entity);

        public bool CanAnchorOn(GridEntity entity, Direction ancor);

        public void AddOccupant(GridEntity entity);
        public void RemoveOccupant(GridEntity entity);

    }
}
