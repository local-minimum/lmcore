using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public interface IDungeonNode
    {
        public Vector3Int Coordinates { get; }

        public MovementOutcome AllowsMovement(GridEntity entity, Direction anchor, Direction direction);
        public bool AllowsEntryFrom(GridEntity entity, Direction direction);
        public bool AllowsRotating(GridEntity entity);

        public bool CanAnchorOn(GridEntity entity, Direction ancor);

        public void Reserve(GridEntity entity);
        public void AddOccupant(GridEntity entity);
        public void RemoveOccupant(GridEntity entity);

    }
}
