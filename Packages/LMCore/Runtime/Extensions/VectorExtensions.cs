using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class VectorExtensions
    {
        public static Vector2Int ToVector2Int(this Vector3 vector, int scale = 3) =>
            new Vector2Int(Mathf.RoundToInt(vector.x / scale), Mathf.RoundToInt(vector.z / scale));
        public static Vector3Int ToVector3Int(this Vector3 vector, int scale = 3) =>
            new Vector3Int(Mathf.RoundToInt(vector.x / scale), Mathf.RoundToInt(vector.y / scale), Mathf.RoundToInt(vector.z / scale));

    }
}
