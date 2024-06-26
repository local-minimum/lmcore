using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class RectExtensions
    {
        /// <summary>
        /// If other is completedly inside this rect
        /// </summary>
        public static bool Contains(this Rect rect, RectInt other) => rect.Contains(other.min) && rect.Contains(other.max);

        public static bool Contains(this Rect rect, Vector2Int point) => rect.Contains(new Vector2(point.x, point.y));

        public static float Area(this Rect rect) => rect.width * rect.height;
    }
}
