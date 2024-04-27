using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class IntRectExtensions
    {
        public static int Area(this RectInt rect) => rect.width * rect.height;

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

        public static bool UnionIsRect(this RectInt rect, RectInt other) =>
            rect.min.x == other.min.x && rect.max.x == other.max.x && (rect.min.y == other.max.y || rect.max.y == other.min.y)
            || rect.min.y == other.min.y && rect.max.y == other.max.y && (rect.min.x == other.max.x || rect.max.x == other.min.x);
    }
}
