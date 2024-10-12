using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    [System.Serializable]
    public enum Direction
    { North, South, West, East, Up, Down, None };

    public enum DirectionAxis { NorthSouth, WestEast, UpDown, None };

    public static class DirectionExtensions
    {
        public static Direction[] AllDirections = new Direction[] { 
            Direction.North, Direction.South, Direction.West, Direction.East, Direction.Up, Direction.Down
        };

        #region Making Directions

        /// <summary>
        /// Convert a look direction to direction enum in the horizonal plane
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown if there's no primary direction</exception>
        public static Direction AsDirection(this Vector2Int lookDirection)
        {
            var cardinal = lookDirection.PrimaryCardinalDirection(false);

            if (cardinal == Vector2Int.left) return Direction.West;
            if (cardinal == Vector2Int.right) return Direction.East;
            if (cardinal == Vector2Int.up) return Direction.North;
            if (cardinal == Vector2Int.down) return Direction.South;

            throw new System.ArgumentException($"${lookDirection} is not a cardinal direction");
        }

        /// <summary>
        /// Convert a look direction to direction
        /// </summary>
        /// <param name="lookDirection"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if there's no primary direction</exception>
        public static Direction AsDirection(this Vector3Int lookDirection)
        {
            var cardinal = lookDirection.PrimaryCardinalDirection(false);

            if (cardinal == Vector3Int.left) return Direction.West;
            if (cardinal == Vector3Int.right) return Direction.East;
            if (cardinal == Vector3Int.up) return Direction.Up;
            if (cardinal == Vector3Int.down) return Direction.Down;
            if (cardinal == Vector3Int.forward) return Direction.North;
            if (cardinal == Vector3Int.back) return Direction.South;

            throw new System.ArgumentException($"{lookDirection} is not a cardinal direction");
        }

        /// <summary>
        /// Convert a look direction to direction
        /// </summary>
        /// <param name="lookDirection"></param>
        /// <returns>The direction if parsasble, else None</returns>
        public static Direction AsDirectionOrNone(this Vector3Int lookDirection)
        {
            var cardinal = lookDirection.PrimaryCardinalDirection(false);

            if (cardinal == Vector3Int.left) return Direction.West;
            if (cardinal == Vector3Int.right) return Direction.East;
            if (cardinal == Vector3Int.up) return Direction.Up;
            if (cardinal == Vector3Int.down) return Direction.Down;
            if (cardinal == Vector3Int.forward) return Direction.North;
            if (cardinal == Vector3Int.back) return Direction.South;

            return Direction.None;
        }

        #endregion Making Directions

        /// <summary>
        /// Rotates direction counter clock-wise
        ///
        /// Note that Up/Down only applicable in 3D
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static Direction RotateCCW(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Direction.West;

                case Direction.West:
                    return Direction.South;

                case Direction.South:
                    return Direction.East;

                case Direction.East:
                    return Direction.North;

                default:
                    throw new System.ArgumentOutOfRangeException($"{direction} is not a planar cardinal");
            }
        }

        /// <summary>
        /// Rotates counter clockwise a direction in 3D space given an up direction
        /// </summary>
        /// <exception cref="System.ArgumentException">If up is on the same axis as direction</exception>
        public static Direction Rotate3DCCW(this Direction direction, Direction down)
        {
            if (direction.IsParallell(down)) throw new System.ArgumentException("Direction can't be parallell to down");

            return direction.AsLookVector3D().RotateCCW(down.Inverse().AsLookVector3D()).AsDirection();
        }

        /// <summary>
        /// Rotates direction clock-wise
        ///
        /// Note that Up/Down only applicable in 3D
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static Direction RotateCW(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Direction.East;

                case Direction.East:
                    return Direction.South;

                case Direction.South:
                    return Direction.West;

                case Direction.West:
                    return Direction.North;

                default:
                    throw new System.ArgumentOutOfRangeException($"{direction} is not a planar cardinal");
            }
        }

        /// <summary>
        /// Rotates clockwise a direction in 3D space given an down direction
        /// </summary>
        /// <exception cref="System.ArgumentException">If up is on the same axis as direction</exception>
        public static Direction Rotate3DCW(this Direction direction, Direction down)
        {
            if (direction.IsParallell(down)) throw new System.ArgumentException("Direction can't be parallell to down");

            return direction.AsLookVector3D().RotateCW(down.Inverse().AsLookVector3D()).AsDirection();
        }

        public static Direction PitchUp(this Direction lookDirection, Direction down, out Direction newDown)
        {
            newDown = lookDirection;
            return down.Inverse();
        }

        public static Direction PitchDown(this Direction lookDirection, Direction down, out Direction newDown)
        {
            newDown = lookDirection.Inverse();
            return down;
        }

        /// <summary>
        /// Flips direction
        /// </summary>
        public static Direction Inverse(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Direction.South;

                case Direction.East:
                    return Direction.West;

                case Direction.South:
                    return Direction.North;

                case Direction.West:
                    return Direction.East;

                case Direction.Up:
                    return Direction.Down;

                case Direction.Down:
                    return Direction.Up;

                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Translates coordinates by direction
        /// </summary>
        public static Vector2Int Translate(this Direction direction, Vector2Int coords)
        {
            switch (direction)
            {
                case Direction.North:
                    return new Vector2Int(coords.x, coords.y + 1);

                case Direction.South:
                    return new Vector2Int(coords.x, coords.y - 1);

                case Direction.West:
                    return new Vector2Int(coords.x - 1, coords.y);

                case Direction.East:
                    return new Vector2Int(coords.x + 1, coords.y);

                default:
                    return coords;
            }
        }

        public static Vector3Int Translate(this Direction direction, Vector3Int coords)
        {
            switch (direction)
            {
                case Direction.North:
                    return new Vector3Int(coords.x, coords.y, coords.z + 1);

                case Direction.South:
                    return new Vector3Int(coords.x, coords.y, coords.z - 1);

                case Direction.West:
                    return new Vector3Int(coords.x - 1, coords.y, coords.z);

                case Direction.East:
                    return new Vector3Int(coords.x + 1, coords.y, coords.z);

                case Direction.Up:
                    return new Vector3Int(coords.x, coords.y + 1, coords.z);

                case Direction.Down:
                    return new Vector3Int(coords.x, coords.y - 1, coords.z);

                default:
                    return coords;
            }
        }

        /// <summary>
        /// Direction as a look direction vector
        /// </summary>
        public static Vector2Int AsLookVector(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return new Vector2Int(0, 1);

                case Direction.South:
                    return new Vector2Int(0, -1);

                case Direction.West:
                    return new Vector2Int(-1, 0);

                case Direction.East:
                    return new Vector2Int(1, 0);

                default:
                    throw new System.ArgumentException($"{direction} is not a planar look vector");
            }
        }

        /// <summary>
        /// Direction as a look direction vector
        /// </summary>
        public static Vector3Int AsLookVector3D(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return new Vector3Int(0, 0, 1);

                case Direction.South:
                    return new Vector3Int(0, 0, -1);

                case Direction.West:
                    return new Vector3Int(-1, 0, 0);

                case Direction.East:
                    return new Vector3Int(1, 0, 0);

                case Direction.Up:
                    return new Vector3Int(0, 1, 0);

                case Direction.Down:
                    return new Vector3Int(0, -1, 0);

                case Direction.None:
                    return Vector3Int.zero;

                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// World space rotation considering
        /// </summary>
        public static Quaternion AsQuaternion(this Direction direction, Direction down, bool is3DSpace = false) {
            if (is3DSpace)
            {
                return direction.AsLookVector3D().AsQuaternion(down.AsLookVector3D());
            }

            if (direction.IsParallell(Direction.Up))
            {
                return down.AsLookVector().AsQuaternion();
            }

            return direction.AsLookVector().AsQuaternion();
        }

        /// <summary>
        /// Return resultant direction after application of rotational movements
        ///
        /// Note that translations returns input direction
        /// </summary>
        public static Direction ApplyRotation(this Direction direction, Direction down,  Movement movement, out Direction newDown)
        {
            switch (movement)
            {
                case Movement.YawCCW:
                    newDown = down;
                    return direction.Rotate3DCCW(down);

                case Movement.YawCW:
                    newDown = down;
                    return direction.Rotate3DCW(down);

                case Movement.PitchUp:
                    return direction.PitchUp(down, out newDown);

                case Movement.PitchDown:
                    return direction.PitchDown(down, out newDown);

                case Movement.RollCCW:
                case Movement.RollCW:
                    Debug.LogError($"Rotation {movement} not supported yet");
                    newDown = down;
                    return direction;

                default:
                    newDown = down;
                    return direction;
            }
        }

        /// <summary>
        /// The direction of a movement using the direction as reference point.
        ///
        /// Note that rotaions returns input direction
        /// </summary>
        public static Direction RelativeTranslation(this Direction direction, Movement movement)
        {
            switch (movement)
            {
                case Movement.Forward:
                    return direction;
                case Movement.Backward:
                    return direction.Inverse();

                case Movement.StrafeLeft:
                    return direction.RotateCCW();

                case Movement.StrafeRight:
                    return direction.RotateCW();

                case Movement.Down:
                    return Direction.Down; 

                case Movement.Up:
                    return Direction.Up;

                default:
                    return Direction.None;
            }
        }

        /// <summary>
        /// The direction of a movement using the direction as reference point.
        ///
        /// Note that rotaions returns input direction
        /// </summary>
        public static Direction RelativeTranslation3D(this Direction direction, Direction down, Movement movement)
        {
            switch (movement)
            {
                case Movement.Forward:
                    return direction;
                case Movement.Backward:
                    return direction.Inverse();

                case Movement.StrafeLeft:
                    return direction.Rotate3DCCW(down);

                case Movement.StrafeRight:
                    return direction.Rotate3DCW(down);

                case Movement.Down:
                    return down; 

                case Movement.Up:
                    return down.Inverse();

                case Movement.AbsNorth:
                    return Direction.North;

                case Movement.AbsSouth:
                    return Direction.South;

                case Movement.AbsWest:
                    return Direction.West;

                case Movement.AbsEast:
                    return Direction.East;

                case Movement.AbsUp: 
                    return Direction.Up;

                case Movement.AbsDown:
                    return Direction.Down;

                default:
                    return Direction.None;
            }
        }

        public static DirectionAxis AsAxis(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                case Direction.South:
                    return DirectionAxis.NorthSouth;
                case Direction.East:
                case Direction.West:
                    return DirectionAxis.WestEast;
                case Direction.Up:
                case Direction.Down:
                    return DirectionAxis.UpDown;
                default:
                    throw new System.ArgumentException("Not a real direction");
            }
        }

        public static Movement AsMovement(this Direction direction, Direction lookDirection, Direction down)
        {
            if (direction == lookDirection) return Movement.Forward;
            if (direction.Inverse() == lookDirection) return Movement.Backward;
            var strafeLeft = lookDirection.Rotate3DCCW(down);
            if (strafeLeft == direction) return Movement.StrafeLeft;
            if (strafeLeft.Inverse() == direction) return Movement.StrafeRight;
            if (direction.PitchDown(down, out Direction _) == direction) return Movement.Down;
            if (direction.PitchUp(down, out Direction _) == direction) return Movement.Up;
            return Movement.None;
        }

        public static Movement AsMovement(this Direction direction) { 
            switch (direction)
            {
                case Direction.North:
                    return Movement.AbsNorth;
                case Direction.South:
                    return Movement.AbsSouth;
                case Direction.West:
                    return Movement.AbsWest;
                case Direction.East:
                    return Movement.AbsEast;
                case Direction.Up:
                    return Movement.AbsUp;
                case Direction.Down:
                    return Movement.AbsDown;
                default:
                    return Movement.None;
            }
        }

        public static bool IsSameAxis(this Direction direction, Direction other) =>
            direction == other || direction == other.Inverse();

        public static bool IsPlanarCardinal(this Direction direction)
        {
            return direction != Direction.Up && direction != Direction.Down && direction != Direction.None;
        }

        public static bool IsParallell(this Direction down, Direction other) =>
            down == other || down.Inverse() == other;

        static List<Direction> CWCircleAroundNorth = new List<Direction> { Direction.Up, Direction.East, Direction.Down, Direction.West };
        static List<Direction> CWCircleAroundWest = new List<Direction> { Direction.Up, Direction.North, Direction.Down, Direction.South };
        static List<Direction> CWCircleAroundUp = new List<Direction> { Direction.North, Direction.West, Direction.South, Direction.East };

        public static bool IsCWRotation(this Direction forward, Direction down, Direction other)
        {
            if (forward.IsParallell(other) || forward.IsParallell(down)) throw new System.ArgumentException(
                $"Forward {forward}, down {down} and direction {other} does not have a clockwise rotation"
            );

            switch (forward)
            {
                case Direction.North:
                case Direction.South:
                    var idxDown = CWCircleAroundNorth.IndexOf(down);
                    var idxOther = CWCircleAroundNorth.IndexOf(other);
                    var isCWNorth = idxDown + 1 == idxOther || idxDown == 3 && idxOther == 0;
                    return forward == Direction.North ? isCWNorth : !isCWNorth;
                case Direction.West:
                case Direction.East:
                    idxDown = CWCircleAroundWest.IndexOf(down);
                    idxOther = CWCircleAroundWest.IndexOf(other);
                    var isCWWest = idxDown + 1 == idxOther || idxDown == 3 && idxOther == 0;
                    return forward == Direction.West ? isCWWest: !isCWWest;
                case Direction.Up:
                case Direction.Down:
                    idxDown = CWCircleAroundUp.IndexOf(down);
                    idxOther = CWCircleAroundUp.IndexOf(other);
                    var isCWUp = idxDown + 1 == idxOther || idxDown == 3 && idxOther == 0;
                    return forward == Direction.West ? isCWUp: !isCWUp;
            }

            return false;
        }

        public static Movement RotationMovementFromCubeInsideDirections(this Direction lookDirection, Direction down, Direction movementDirection)
        {
            if (lookDirection == movementDirection) return Movement.PitchUp;
            if (lookDirection == movementDirection.Inverse()) return Movement.PitchDown;
            if (!down.IsParallell(movementDirection))
            {
                return IsCWRotation(lookDirection, down, movementDirection) ? Movement.RollCW : Movement.RollCCW;
            }
            return Movement.None;
        }
    }
}