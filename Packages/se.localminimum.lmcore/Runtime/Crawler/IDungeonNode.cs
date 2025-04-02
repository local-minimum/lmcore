using LMCore.IO;
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
            bool checkOccupancyRule = true,
            bool withPush = false);

        public bool AllowsEntryFrom(
            GridEntity entity,
            Direction direction,
            bool checkOccupancyRules = true,
            bool withPush = false);
        public bool AllowExit(GridEntity entity, Direction direction, bool forced);
        public bool AllowsRotation(GridEntity entity, Movement rotation);

        public bool CanAnchorOn(GridEntity entity, Direction anchor);

        /// <summary>
        /// If occupancy rules and current occupants / reservations allow
        /// entry to node.
        /// </summary>
        /// <param name="entity">The new entity</param>
        /// <param name="pushDirection">The direction the pusher uses to enter node</param>
        /// <param name="checkPush">If we should try to circumvent occupation and reservation rules</param>
        /// <returns></returns>
        public bool MayInhabit(GridEntity entity, Direction pushDirection, bool checkPush);
        public void Reserve(GridEntity entity);
        public void RemoveReservation(GridEntity entity);
        public void AddOccupant(GridEntity entity, bool push);
        public void RemoveOccupant(GridEntity entity);

        bool PushOccupants(GridEntity activeEntity, Direction pushDirection);

        /// <summary>
        /// Returns the coordinates of the node in the given direction
        /// </summary>
        public Vector3Int Neighbour(Direction direction);

        public bool HasFloor { get; }
        public bool HasIllusorySurface(Direction direction);
        public bool HasCubeFace(Direction direction);

        public bool Walkable { get; }
        public bool Flyable { get; }
    }
}
