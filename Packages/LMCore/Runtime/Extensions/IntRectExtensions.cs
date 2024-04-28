using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class IntRectExtensions
    {
        public static int Area(this RectInt rect) => rect.width * rect.height;

        /// <summary>
        /// Apply action over all coordinates of the rect excluding max
        /// </summary>
        public static void ApplyForRect(this RectInt rect, System.Action<int, int> action)
        {
            for (int y = rect.min.y, yMax = rect.max.y; y < yMax; y++)
            {
                for (int x = rect.min.x, xMax = rect.max.x; x < xMax; x++)
                {
                    action(x, y);
                }
            }
        }

        /// <summary>
        /// Makes a union rect of the bounding box for the two rects
        /// </summary>
        public static RectInt Union(this RectInt rect, RectInt other)
        {
            var min = Vector2Int.Min(rect.min, other.min);
            var max = Vector2Int.Max(rect.max, other.max);
            return new RectInt(min, max - min);
        }

        /// <summary>
        /// Answers if the union of the two rects includes any area outside either rect
        /// </summary>
        public static bool UnionIsRect(this RectInt rect, RectInt other)
        {
            var union = rect.Union(other);

            if (union.Equals(rect) || union.Equals(other)) return true;

            var uMin = union.min;
            if (uMin != rect.min && uMin != other.min) return false;

            var uSize = union.size;
            var rSize = rect.size;
            var oSize = other.size; 

            return uSize.x == rSize.x && uSize.x == oSize.x && uSize.y <= rSize.y + oSize.y
                || uSize.y == rSize.y && uSize.y == oSize.y && uSize.x <= rSize.x + oSize.x;
        }           
    }
}
