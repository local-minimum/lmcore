using LMCore.Crawler;
using System.Collections.Generic;
using TiledImporter;

namespace TiledDungeon.Integration
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
    }
}
