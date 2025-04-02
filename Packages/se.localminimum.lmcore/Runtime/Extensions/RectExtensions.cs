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

        public static Vector3 Clamp(this Rect rect, Vector3 pos) =>
            new Vector3(Mathf.Clamp(pos.x, rect.xMin, rect.xMax), Mathf.Clamp(pos.y, rect.yMin, rect.yMax));

        public static Vector2 Clamp(this Rect rect, Vector2 pos) =>
            new Vector2(Mathf.Clamp(pos.x, rect.xMin, rect.xMax), Mathf.Clamp(pos.y, rect.yMin, rect.yMax));
    }
}
