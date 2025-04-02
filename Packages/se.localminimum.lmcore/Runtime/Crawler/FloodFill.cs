using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public static class FloodFill
    {
        public static bool TransitionFilter(IDungeonNode from, Direction direction, IDungeonNode to)
        {
            return direction == Direction.None ||
               (from != null &&
               to != null &&
               (from.HasIllusorySurface(direction) || !from.HasCubeFace(direction)) &&
               (to.HasIllusorySurface(direction.Inverse()) || !to.HasCubeFace(direction.Inverse())));
        }
        static IEnumerable<KeyValuePair<Direction, Direction>> CreateDirectionsWithDiagonals()
        {
            for (int i = 0, n = DirectionExtensions.AllDirections.Length; i < n; i++)
            {
                var direction = DirectionExtensions.AllDirections[i];
                yield return new KeyValuePair<Direction, Direction>(direction, Direction.None);

                for (int j = 0; j < n; j++)
                {
                    var secondary = DirectionExtensions.AllDirections[j];
                    if (secondary.IsParallell(direction)) continue;

                    yield return new KeyValuePair<Direction, Direction>(direction, secondary);
                }
            }
        }

        private static List<KeyValuePair<Direction, Direction>> _DirectionsWithDiagonals = null;

        static List<KeyValuePair<Direction, Direction>> DirectionsWithDiagonals
        {
            get
            {
                if (_DirectionsWithDiagonals == null)
                {
                    _DirectionsWithDiagonals = CreateDirectionsWithDiagonals().ToList();
                }
                return _DirectionsWithDiagonals;
            }
        }

        public static IEnumerable<Vector3Int> Fill(IDungeon dungeon, Vector3Int origin, int depth) =>
            Fill(dungeon, origin, depth, TransitionFilter);

        public static IEnumerable<Vector3Int> Fill(IDungeon dungeon, Vector3Int origin, int depth, System.Func<IDungeonNode, Direction, IDungeonNode, bool> filter)
        {
            var depths = new Dictionary<Vector3Int, int>();
            var seen = new Queue<Vector3Int>();
            seen.Enqueue(origin);
            var directions = DirectionsWithDiagonals;

            int n = 1;
            while (n > 0)
            {
                var coordinates = seen.Dequeue();
                n--;

                yield return coordinates;

                var myDepth = depths.GetValueOrDefault(coordinates);

                if (myDepth < depth)
                {
                    var node = dungeon[coordinates];
                    foreach (var (direction, secondary) in directions)
                    {
                        var intermediateCoordinates = direction.Translate(coordinates);
                        var targetCoordinates = secondary.Translate(intermediateCoordinates);

                        if (depths.ContainsKey(targetCoordinates)) continue;

                        var intermediate = dungeon[intermediateCoordinates];
                        var target = dungeon[targetCoordinates];
                        if (filter(node, direction, intermediate) && (secondary == Direction.None || filter(intermediate, secondary, target)))
                        {
                            depths[targetCoordinates] = myDepth + 1;
                            seen.Enqueue(targetCoordinates);
                            n++;
                        }
                    }
                }
            }
        }
    }
}
