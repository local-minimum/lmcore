using LMCore.IO;
using UnityEngine;

namespace LMCore.Crawler
{
    public delegate void EntityMovementEvent(
        GridEntity entity, 
        Movement movement, 
        Vector3Int startPosition, 
        Direction startDirection, 
        Vector3Int endPosition, 
        Direction endDirection,
        bool allowed 
    );

    public interface IEntityMover
    {
        public event EntityMovementEvent OnMoveStart;
        public event EntityMovementEvent OnMoveEnd;

        public IGridSizeProvider GridSizeProvider { set; }
        public bool Enabled { get; }
    }
}
