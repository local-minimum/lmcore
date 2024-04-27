using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LMCore.Extensions
{
    public static class IntVectorExtensions
    {
        public static readonly Vector2Int[] Cardinal2DVectors = new Vector2Int[] {
            Vector2Int.left, Vector2Int.up, Vector2Int.right, Vector2Int.down,
        };

        public static readonly Vector3Int[] Cardinal3DVectors = new Vector3Int[]
        {
            Vector3Int.left, Vector3Int.forward, Vector3Int.right, Vector3Int.back, Vector3Int.up, Vector3Int.down,
        };

        public static Vector2Int Random2DDirection() => Cardinal2DVectors[Random.Range(0, 4)];
        public static Vector3Int Random3DDirection() => Cardinal3DVectors[Random.Range(0, 6)];

        #region Axis
        public static int SmallestAxisMagnitude(this Vector2Int vector) => Mathf.Min(Mathf.Abs(vector.x), Mathf.Abs(vector.y));
        public static int SmallestAxisMagnitude(this Vector3Int vector) => Mathf.Min(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

        public static int LargestAxisMagnitude(this Vector2Int vector) => Mathf.Max(Mathf.Abs(vector.x), Mathf.Abs(vector.y));
        public static int LargetsAxisMagnitude(this Vector3Int vector) => Mathf.Max(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

        public static Vector2Int[] AsUnitComponents(this Vector2Int direction) => new Vector2Int[] {
            new Vector2Int(direction.x.Sign(), 0),
            new Vector2Int(0, direction.y.Sign()),
        };
        public static Vector3Int[] AsUnitComponents(this Vector3Int direction) => new Vector3Int[] {
            new Vector3Int(direction.x.Sign(), 0, 0),
            new Vector3Int(0, direction.y.Sign(), 0),
            new Vector3Int(0, 0, direction.z.Sign()),
        };
        #endregion

        public static bool IsUnitVector(this Vector2Int vector) =>
            Mathf.Abs(vector.x) + Mathf.Abs(vector.y) == 1;
        public static bool IsUnitVector(this Vector3Int vector) =>
            Mathf.Abs(vector.x) + Mathf.Abs(vector.y) + Mathf.Abs(vector.z) == 1;
        #region Cardinality
        public static Vector2Int PrimaryCardinalDirection(this Vector2Int origin, Vector2Int target, bool resolveIndeterminateByRng = true) => PrimaryCardinalDirection(target - origin, resolveIndeterminateByRng);
        public static Vector2Int PrimaryCardinalDirection(this Vector2Int direction, bool resolveIndeterminateByRng = true)
        {
            var x = Mathf.Abs(direction.x);
            var y = Mathf.Abs(direction.y);

            if (x > y)
            {
                return direction.x.Sign() == 1 ? Vector2Int.right : Vector2Int.left;
            }

            if (y > x)
            {
                return direction.y.Sign() == 1 ? Vector2Int.up : Vector2Int.down;
            }

            if (resolveIndeterminateByRng)
            {
                if (Random.value < 0.5f)
                {
                    return direction.x.Sign() == 1 ? Vector2Int.right : Vector2Int.left;
                }

                return direction.y.Sign() == 1 ? Vector2Int.up : Vector2Int.down;
            }

            return Vector2Int.zero;
        }
        public static Vector3Int PrimaryCardinalDirection(this Vector3Int origin, Vector3Int target, bool resolveIndeterminateByRng = true) => PrimaryCardinalDirection(target - origin, resolveIndeterminateByRng);
        public static Vector3Int PrimaryCardinalDirection(this Vector3Int direction, bool resolveIndeterminateByRng = true)
        {
            var x = Mathf.Abs(direction.x);
            var y = Mathf.Abs(direction.y);
            var z = Mathf.Abs(direction.z);

            if (x > y && x > z)
            {
                return direction.x.Sign() == 1 ? Vector3Int.right : Vector3Int.left;
            }

            if (y > x && y > z)
            {
                return direction.y.Sign() == 1 ? Vector3Int.up : Vector3Int.down;
            }

            if (z > x && z > y)
            {
                return direction.z.Sign() == 1 ? Vector3Int.forward : Vector3Int.back;
            }

            if (resolveIndeterminateByRng)
            {
                if (z < y)
                {
                    if (Random.value < 0.5f)
                    {
                        return direction.x.Sign() == 1 ? Vector3Int.right : Vector3Int.left;
                    }

                    return direction.y.Sign() == 1 ? Vector3Int.up : Vector3Int.down;
                }

                if (x < y)
                {
                    if (Random.value < 0.5f)
                    {
                        return direction.z.Sign() == 1 ? Vector3Int.forward : Vector3Int.back;
                    }

                    return direction.y.Sign() == 1 ? Vector3Int.up : Vector3Int.down;
                }

                if (y < z)
                {
                    if (Random.value < 0.5f)
                    {
                        return direction.z.Sign() == 1 ? Vector3Int.forward : Vector3Int.back;
                    }

                    return direction.x.Sign() == 1 ? Vector3Int.right : Vector3Int.left;
                }

                int r = Random.Range(0, 3);
                if (r == 0)
                {
                    return direction.x.Sign() == 1 ? Vector3Int.right : Vector3Int.left;
                }
                if (r == 1)
                {
                    return direction.y.Sign() == 1 ? Vector3Int.up : Vector3Int.down;
                }

                return direction.z.Sign() == 1 ? Vector3Int.forward : Vector3Int.back;
            }

            return Vector3Int.zero;
        }

        public static bool IsCardinal(this Vector2Int vector) => (vector.x == 0) != (vector.y == 0);
        public static bool IsCardinal(this Vector3Int vector) =>
            Mathf.Abs(vector.x.Sign()) + Mathf.Abs(vector.y.Sign()) + Mathf.Abs(vector.z.Sign()) == 1;

        public static bool IsOrthogonalCardinal(this Vector2Int cardinal1, Vector2Int cardinal2, bool trustCardinality = true) =>
             cardinal1.x == 0 && cardinal2.x != 0 && (trustCardinality || cardinal1.IsCardinal() && cardinal2.IsCardinal());
        public static bool IsOrthogonalCardinal(this Vector3Int cardinal1, Vector3Int cardinal2, bool trustCardinality = true) =>
             (cardinal1.x == 0 && cardinal2.x != 0 || cardinal1.y == 0 && cardinal2.y != 0) && (trustCardinality || cardinal1.IsCardinal() && cardinal2.IsCardinal());

        public static bool IsInverseDirection(this Vector2Int direction1, Vector2Int direction2) =>
            direction1.x == -direction2.x && direction1.y == -direction2.y;
        public static bool IsInverseDirection(this Vector3Int direction1, Vector3Int direction2) =>
            direction1.x == -direction2.x && direction1.y == -direction2.y && direction1.z == -direction2.z;
        #endregion

        #region Rotations
        public static Quaternion AsQuaternion(this Vector2Int direction) => Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
        public static Quaternion AsQuaternion(this Vector3Int direction) => Quaternion.LookRotation(new Vector3(direction.x, direction.y, direction.z));

        public static Vector2Int RotateCCW(this Vector2Int direction) =>
            new Vector2Int(-direction.y, direction.x);
        public static Vector3Int RotateCCW(this Vector3Int direction, Vector3Int up)
        {
            if (up.y > 0) return new Vector3Int(-direction.z, direction.y, direction.x);
            if (up.y < 0) return new Vector3Int(direction.z, direction.y, -direction.x);
            if (up.x > 0) return new Vector3Int(direction.x, -direction.z, direction.y);
            if (up.x < 0) return new Vector3Int(direction.x, direction.z, -direction.y);
            if (up.z > 0) return new Vector3Int(-direction.y, direction.x, direction.z);
            return new Vector3Int(direction.y, -direction.x, direction.z);
        }

        public static Vector2Int RotateCW(this Vector2Int direction) =>
            new Vector2Int(direction.y, -direction.x);
        public static Vector3Int RotateCW(this Vector3Int direction, Vector3Int up) => 
            direction.RotateCW(up * -1);


        public static bool IsCWRotationOf(this Vector2Int cardinal1, Vector2Int cardinal2) =>
            cardinal1.RotateCW() == cardinal2;
        public static bool IsCWRotationOf(this Vector3Int cardinal1, Vector3Int cardinal2, Vector3Int up) =>
            cardinal1.RotateCW(up) == cardinal2;

        public static bool IsCCWRotationOf(this Vector2Int cardinal1, Vector2Int cardinal2) =>
            cardinal1.RotateCCW() == cardinal2;
        public static bool IsCCWRotationOf(this Vector3Int cardinal1, Vector3Int cardinal2, Vector3Int up) =>
            cardinal1.RotateCCW(up) == cardinal2;

        #endregion

        #region Distances
        public static int ManhattanDistance(this Vector2Int point, Vector2Int other) =>
                Mathf.Abs(point.x - other.x) + Mathf.Abs(point.y - other.y);
        public static int ManhattanDistance(this Vector3Int point, Vector3Int other) =>
                Mathf.Abs(point.x - other.x) + Mathf.Abs(point.y - other.y) + Mathf.Abs(point.z - other.z);

        public static int ChebyshevDistance(this Vector2Int point, Vector2Int other) =>
            Mathf.Max(Mathf.Abs(point.x - other.x), Mathf.Abs(point.y - other.y));
        public static int ChebyshevDistance(this Vector3Int point, Vector3Int other) =>
            Mathf.Max(Mathf.Abs(point.x - other.x), Mathf.Abs(point.y - other.y), Mathf.Abs(point.z - other.z));
        #endregion

        #region Intersection
        public static Vector2Int OrthoIntersection(this Vector2Int point, Vector2Int target, Vector2Int direction)
        {
            var candidate = new Vector2Int(point.x, target.y);
            var diff = candidate - point;
            if (diff.x * direction.x + diff.y * direction.y == 0) return candidate;

            return new Vector2Int(target.x, point.y);
        }
        #endregion

        #region World
        public static Vector3 ToPosition(this Vector2Int coords, int elevation = 0, int scale = 3) =>
            new Vector3(coords.x * scale, elevation * scale, coords.y * scale);
        public static Vector3 ToPosition(this Vector3Int coords, int scale = 3) =>
            new Vector3(coords.x * scale, coords.y * scale, coords.z * scale);

        public static Vector3 ToDirection(this Vector2Int direction) => new Vector3(direction.x, 0, direction.y);
        public static Vector3 ToDirection(this Vector3Int direction) => new Vector3(direction.x, direction.y, direction.z);
        #endregion
    }
}
