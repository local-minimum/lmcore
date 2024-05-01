using LMCore.Extensions;
using LMCore.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public enum Direction { North, South, West, East };

    public static class DirectionExtensions
    {
        /// <summary>
        /// Rotates direction counter clock-wise
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
        /// Rotates direction clock-wise
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
                    return Vector2Int.zero;
            }
        }

        /// <summary>
        /// World space rotation
        /// </summary>
        public static Quaternion AsQuaternion(this Direction direction) => 
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
