using LMCore.IO;
using UnityEngine;

namespace LMCore.Crawler
{
    public delegate void EntityMovementStartEvent(
        GridEntity entity, 
        Movement movement, 
        Vector3Int endPosition, 
        Direction endLookDirection, 
        Direction endAnchor,
        bool allowed
    );

    public delegate void EntityMovementEndEvent(
        GridEntity enity,
        bool successful
    );

    public interface IEntityMover 
    {
        public event EntityMovementStartEvent OnMoveStart;
        public event EntityMovementEndEvent OnMoveEnd;

        public IGridSizeProvider GridSizeProvider { set; }
        public IDungeon Dungeon { set; }
        public bool Enabled { get; }
    }
}
