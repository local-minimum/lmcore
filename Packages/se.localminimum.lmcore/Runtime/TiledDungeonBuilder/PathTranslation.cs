using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.TiledDungeon
{
    [System.Serializable]
    public struct PathTranslation
    {
        public Direction TranslationHere;
        public PathCheckpoint Checkpoint;

        public override string ToString() =>
            $"[{TranslationHere} -> {Checkpoint}]";
    }

    public static class PathTranslationExtensions
    {
        public static int Cost(this List<PathTranslation> path, Direction startLook)
        {
            if (path == null) return 0;

            var n = path.Count;
            if (n <= 1) return 0;

            var cost = 0;
            var lastDirection = startLook;
            for (int i = 1; i < n; i++)
            {
                cost++;

                var currentDirection = path[i].TranslationHere;
                if (lastDirection != currentDirection)
                {
                    // Two turns when opposing directions
                    if (lastDirection.Inverse() == currentDirection) cost++;
                    cost++;
                }

                lastDirection = currentDirection;
            }

            return cost;
        }

        public static string Debug(this List<PathTranslation> path) =>
            $"<{string.Join(" => ", path)}>";

        /// <summary>
        /// Extends a path with a final single translation to the traget entity
        /// </summary>
        public static void Extend(this List<PathTranslation> flankPath, GridEntity target)
        {
            var flankPosition = flankPath.Last();
            flankPath.Add(new PathTranslation()
            {
                Checkpoint = new PathCheckpoint() { Anchor = target.AnchorDirection, Coordinates = target.Coordinates },
                TranslationHere = (target.Coordinates - flankPosition.Checkpoint.Coordinates).AsDirection()
            });
        }
    }
}
