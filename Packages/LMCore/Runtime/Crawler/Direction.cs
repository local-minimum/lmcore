using LMCore.Extensions;
using LMCore.IO;
using UnityEngine;

namespace LMCore.Crawler
{
    public enum Direction { North, South, West, East, Up, Down };

    public static class DirectionExtensions
    {
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

            throw new System.ArgumentException($"${lookDirection} is not a cardinal direction");
        }
        #endregion

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
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Rotates counter clockwise a direction in 3D space given an up direction
        /// </summary>
        /// <exception cref="System.ArgumentException">If up is on the same axis as direction</exception>
        public static Direction Rotate3DCCW(this Direction direction, Direction up)
        {
            if (direction == up) throw new System.ArgumentException("Direction can't be same as up");
            if (direction.Inverse() == up) throw new System.ArgumentException("Direction can't be inverse of up");

            return direction.AsLookVector3D().RotateCCW(up.AsLookVector3D()).AsDirection();
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
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Rotates clockwise a direction in 3D space given an up direction
        /// </summary>
        /// <exception cref="System.ArgumentException">If up is on the same axis as direction</exception>
        public static Direction Rotate3DCW(this Direction direction, Direction up)
        {
            if (direction == up) throw new System.ArgumentException("Direction can't be same as up");
            if (direction.Inverse() == up) throw new System.ArgumentException("Direction can't be inverse of up");

            return direction.AsLookVector3D().RotateCW(up.AsLookVector3D()).AsDirection();
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
                    throw new System.ArgumentException();
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
                default:
                    throw new System.ArgumentOutOfRangeException();

            }
        }

        /// <summary>
        /// World space rotation considering
        /// </summary>
        public static Quaternion AsQuaternion(this Direction direction, bool is3DSpace = false) => 
            is3DSpace ? 
            direction.AsLookVector3D().AsQuaternion() :
            direction.AsLookVector().AsQuaternion();


        /// <summary>
        /// Return resultant direction after application of rotational movements
        /// 
        /// Note that translations returns input direction
        /// </summary>
        public static Direction ApplyRotation(this Direction direction, Movement movement)
        {
            switch (movement)
            {
                case Movement.TurnCCW:
                    return direction.RotateCCW();
                case Movement.TurnCW:
                    return direction.RotateCW();
                default:
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
                case Movement.Backward:
                    return direction.Inverse();
                case Movement.StrafeLeft:
                    return direction.RotateCCW();
                case Movement.StrafeRight:
                    return direction.RotateCW();
                default:
                    return direction;
            }
        }
    }
}
