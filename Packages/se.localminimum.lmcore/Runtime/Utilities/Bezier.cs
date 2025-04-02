using UnityEngine;

namespace LMCore.Utilities
{
    public static class Bezier
    {
        /// <summary>
        /// Returns the interpolated point between two anchors
        /// </summary>
        public static Vector3 LerpPoint(Vector3 anchor1, Vector3 control1, Vector3 control2, Vector3 anchor2, float t)
        {
            t = Mathf.Clamp01(t);
            return (Mathf.Pow(1 - t, 3) * anchor1) +
                (3 * Mathf.Pow(1 - t, 2) * t * control1) +
                (3 * (1 - t) * Mathf.Pow(t, 2) * control2) +
                (Mathf.Pow(t, 3) * anchor2);
        }
    }
}
