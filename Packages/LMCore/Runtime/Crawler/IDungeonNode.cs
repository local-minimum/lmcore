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
        public bool AllowsEntryFrom(GridEntity entity, Direction direction, bool checkOccupancyRules = true);
        public bool AllowExit(GridEntity entity, Direction direction);
        public bool AllowsRotating(GridEntity entity);

        public bool CanAnchorOn(GridEntity entity, Direction anchor);

        /// <summary>
        /// If occupancy rules and current occupants / reservations allow
        /// enty to join node 
        /// </summary>
        /// <param name="entity">The new entity</param>
        /// <param name="push">If we should attempt to push anything in there</param>
        /// <returns></returns>
        public bool MayInhabit(GridEntity entity, bool push = true);
        public void Reserve(GridEntity entity);
        public void RemoveReservation(GridEntity entity);
        public void AddOccupant(GridEntity entity);
        public void RemoveOccupant(GridEntity entity);

        /// <summary>
        /// Returns the coordinates of the node in the given direction
        /// </summary>
        public Vector3Int Neighbour(Direction direction);

        public bool HasFloor { get; }
        public bool HasIllusorySurface(Direction direction);  

        public bool Walkable {  get; }
        public bool Flyable { get; }
    }
}
