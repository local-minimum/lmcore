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

        /// <summary>
        /// Validate what happens if the entity attempts a movement in a certain direction.
        /// 
        /// Checking both node exits and node entries.
        /// Also accounts for intermediary nodes when rounding corners and such
        /// </summary>
        /// <returns>What type of movement this would be if any</returns>
        public MovementOutcome AllowsTransition(
            GridEntity entity,
            Vector3Int origin,
            Direction originAnchorDirection,
            Direction direction,
            out Vector3Int targetCoordinates,
            out Anchor targetAnchor,
            bool checkOccupancyRule = true);
        public MovementOutcome AllowsMovement(GridEntity entity, Direction anchor, Direction direction);
        public bool AllowsEntryFrom(GridEntity entity, Direction direction, bool checkOccupancyRules = true);
        public bool AllowExit(GridEntity entity, Direction direction);
        public bool AllowsRotating(GridEntity entity);

        public bool CanAnchorOn(GridEntity entity, Direction anchor);

        public void Reserve(GridEntity entity);
        public void RemoveReservation(GridEntity entity);
        public void AddOccupant(GridEntity entity);
        public void RemoveOccupant(GridEntity entity);

        /// <summary>
        /// Returns the coordinates of the node in the given direction
        /// </summary>
        public Vector3Int Neighbour(Direction direction);

        /// <summary>
        /// Returns the coordinates of the node in the given direction based
        /// on what cube side the entity is attached to or not.
        /// </summary>
        public Vector3Int Neighbour(GridEntity entity, Direction direction, out Anchor targetAnchor);

        public bool HasFloor { get; }
        public bool HasIllusorySurface(Direction direction);  

        public bool Walkable {  get; }
        public bool Flyable { get; }
    }
}
