using UnityEngine;

namespace LMCore.Crawler
{
    public interface IDungeonNode
    {
        public Vector3Int Coordinates { get; }
        public Vector3 CenterPosition { get; }

        public Vector3 GetEdge(Direction anchor);
        public Vector3 GetEdge(Direction anchor, Direction edge);

        public Anchor GetAnchor(Direction direction);

        public MovementOutcome AllowsMovement(GridEntity entity, Direction anchor, Direction direction);
        public bool AllowsEntryFrom(GridEntity entity, Direction direction);
        public bool AllowExit(GridEntity entity, Direction direction);
        public bool AllowsRotating(GridEntity entity);

        public bool CanAnchorOn(GridEntity entity, Direction anchor);

        public void Reserve(GridEntity entity);
        public void AddOccupant(GridEntity entity);
        public void RemoveOccupant(GridEntity entity);

        public Vector3Int Neighbour(Direction direction);

        public bool IsHighRamp { get; }
        public bool IsRamp { get; }

        public bool HasFloor { get; }
        public bool HasIllusorySurface(Direction direction);  

        /*
        public void AssignConstraints(GridEntity entity, Direction direction);
        public void RemoveConstraints(GridEntity entity, Direction direction);
        */
    }
}
