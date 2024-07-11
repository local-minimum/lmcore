using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public delegate void EntityMovementStartEvent(
        GridEntity entity, 
        List<Vector3Int> positions
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
        public bool Animating { get; }
        public void EndAnimation(bool emitEndEvent = true);
    }
}
