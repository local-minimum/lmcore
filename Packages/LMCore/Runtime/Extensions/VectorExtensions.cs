using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class VectorExtensions
    {
        /// <summary>
        /// Convert a world vector to the int vector space
        /// </summary>
        /// <param name="scale">Size of each int vector step in world space</param>
        public static Vector2Int ToVector2IntXZPlane(this Vector3 vector, int scale = 3) =>
            new Vector2Int(Mathf.RoundToInt(vector.x / scale), Mathf.RoundToInt(vector.z / scale));
        /// <summary>
        /// Convert a world vector to the int vector space
        /// </summary>
        /// <param name="scale">Size of each int vector step in world space</param>
        public static Vector3Int ToVector3Int(this Vector3 vector, int scale = 3) =>
            new Vector3Int(Mathf.RoundToInt(vector.x / scale), Mathf.RoundToInt(vector.y / scale), Mathf.RoundToInt(vector.z / scale));

    }
}
