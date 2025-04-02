using LMCore.Crawler;
using LMCore.TiledImporter;
using System.Collections.Generic;

namespace LMCore.TiledDungeon.Integration
{
    [System.Serializable]
    public class TDSidesClass
    {
        public bool North;
        public bool South;
        public bool West;
        public bool East;
        public bool Up;
        public bool Down;

        public TDSidesClass() { }
        public TDSidesClass(bool north, bool south, bool west, bool east, bool up, bool down)
        {
            North = north;
            South = south;
            West = west;
            East = east;
            Up = up;
            Down = down;
        }
        public TDSidesClass(TDSidesClass source)
        {
            North = source.North;
            South = source.South;
            West = source.West;
            East = source.East;
            Up = source.Up;
            Down = source.Down;
        }

        public static TDSidesClass From(TiledCustomClass sides, TiledNodeRoofRule roofRule)
        {
            if (sides == null) return new TDSidesClass();

            var up = roofRule == TiledNodeRoofRule.CustomProps ? sides.Bool("Up") : roofRule == TiledNodeRoofRule.ForcedSet;

            return new TDSidesClass()
            {
                North = sides.Bool("North"),
                South = sides.Bool("South"),
                West = sides.Bool("West"),
                East = sides.Bool("East"),
                Up = up,
                Down = sides.Bool("Down")
            };
        }

        public bool Has(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return North;
                case Direction.South:
                    return South;
                case Direction.West:
                    return West;
                case Direction.East:
                    return East;
                case Direction.Up:
                    return Up;
                case Direction.Down:
                    return Down;
                default:
                    return false;
            }
        }

        public void Set(Direction direction, bool value)
        {
            switch (direction)
            {
                case Direction.North:
                    North = value;
                    break;
                case Direction.South:
                    South = value;
                    break;
                case Direction.West:
                    West = value;
                    break;
                case Direction.East:
                    East = value;
                    break;
                case Direction.Up:
                    Up = value;
                    break;
                case Direction.Down:
                    Down = value;
                    break;
            }
        }

        public IEnumerable<Direction> Directions
        {
            get
            {
                if (Has(Direction.North)) yield return Direction.North;
                if (Has(Direction.South)) yield return Direction.South;
                if (Has(Direction.West)) yield return Direction.West;
                if (Has(Direction.East)) yield return Direction.East;
                if (Has(Direction.Up)) yield return Direction.Up;
                if (Has(Direction.Down)) yield return Direction.Down;
            }
        }
    }
}
