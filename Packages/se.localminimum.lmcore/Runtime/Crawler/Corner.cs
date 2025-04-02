using System.Collections.Generic;

namespace LMCore.Crawler
{
    public enum Corner
    {
        None,
        NorthWest,
        NorthEast,
        SouthEast,
        SouthWest
    }

    public static class CornerExtensions
    {
        public static IEnumerable<Direction> Directions(this Corner corner)
        {
            switch (corner)
            {
                case Corner.NorthWest:
                    yield return Direction.North;
                    yield return Direction.West;
                    break;
                case Corner.NorthEast:
                    yield return Direction.North;
                    yield return Direction.East;
                    break;
                case Corner.SouthEast:
                    yield return Direction.South;
                    yield return Direction.East;
                    break;
                case Corner.SouthWest:
                    yield return Direction.South;
                    yield return Direction.West;
                    break;

            }
        }
    }
}
