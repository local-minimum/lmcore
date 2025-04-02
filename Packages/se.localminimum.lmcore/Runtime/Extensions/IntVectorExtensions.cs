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

        public static Vector3Int Abs(this Vector3Int v) => new Vector3Int(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        public static Vector2Int Abs(this Vector2Int v) => new Vector2Int(Mathf.Abs(v.x), Mathf.Abs(v.y));

        /// <summary>
        /// Transform a relative vector in the coordinate space given by the forward and down
        /// vectors into the standard x,y,z coordinate space.
        /// </summary>
        /// <param name="vector">A relative vector with x as right, y as up and z as forward</param>
        /// <param name="forward">Direction of forward in the coordinate space</param>
        /// <param name="down">Direction of down in the coordinate space</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static Vector3Int RelativeFrame(this Vector3Int vector, Vector3Int forward, Vector3Int down)
        {
            if (!forward.IsUnitVector() || !down.IsUnitVector())
            {
                throw new System.ArgumentException($"Forward {forward} and {down} must be unit vectors.");
            }

            return (forward * vector.z) +
                (-1 * down * vector.y) +
                ((Vector3Int.one - forward.Abs() - down.Abs()) * vector.x);
        }

        #region Axis

        public static int SmallestAxisMagnitude(this Vector2Int vector) => Mathf.Min(Mathf.Abs(vector.x), Mathf.Abs(vector.y));

        public static int SmallestAxisMagnitude(this Vector3Int vector) => Mathf.Min(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

        public static int LargestAxisMagnitude(this Vector2Int vector) => Mathf.Max(Mathf.Abs(vector.x), Mathf.Abs(vector.y));

        public static int LargetsAxisMagnitude(this Vector3Int vector) => Mathf.Max(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

        public static Vector2Int[] AsUnitComponents(this Vector2Int direction) =>
            direction.IsUnitVector() ?
            new Vector2Int[] { direction } :
            new Vector2Int[] {
                new Vector2Int(direction.x.Sign(), 0),
                new Vector2Int(0, direction.y.Sign()),
            };

        public static Vector3Int[] AsUnitComponents(this Vector3Int direction) =>
            direction.IsUnitVector() ?
            new Vector3Int[] { direction } :
            new Vector3Int[] {
                new Vector3Int(direction.x.Sign(), 0, 0),
                new Vector3Int(0, direction.y.Sign(), 0),
                new Vector3Int(0, 0, direction.z.Sign()),
            };

        #endregion Axis

        public static bool IsUnitVector(this Vector2Int vector) =>
            Mathf.Abs(vector.x) + Mathf.Abs(vector.y) == 1;

        public static bool IsUnitVector(this Vector3Int vector) =>
            Mathf.Abs(vector.x) + Mathf.Abs(vector.y) + Mathf.Abs(vector.z) == 1;

        #region Cardinality

        /// <summary>
        /// Returns unit vector along the primary carninal axis.
        ///
        /// If indeterminate it will either return the zero vector or random selected among candidates of equal length
        /// </summary>
        public static Vector2Int PrimaryCardinalDirection(this Vector2Int origin, Vector2Int target, bool resolveIndeterminateByRng = true) => PrimaryCardinalDirection(target - origin, resolveIndeterminateByRng);

        /// <summary>
        /// Returns unit vector along the primary carninal axis.
        ///
        /// If indeterminate it will either return the zero vector or random selected among candidates of equal length
        /// </summary>
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

        /// <summary>
        /// Returns unit vector along the primary carninal axis.
        ///
        /// If indeterminate it will either return the zero vector or random selected among candidates of equal length
        /// </summary>
        public static Vector3Int PrimaryCardinalDirection(this Vector3Int origin, Vector3Int target, bool resolveIndeterminateByRng = true) => PrimaryCardinalDirection(target - origin, resolveIndeterminateByRng);

        /// <summary>
        /// Returns unit vector along the primary carninal axis.
        ///
        /// If indeterminate it will either return the zero vector or random selected among candidates of equal length
        /// </summary>
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

        public static bool IsCardinal(this Vector2Int vector) => vector.x == 0 != (vector.y == 0);

        public static bool IsCardinal(this Vector3Int vector) =>
            Mathf.Abs(vector.x.Sign()) + Mathf.Abs(vector.y.Sign()) + Mathf.Abs(vector.z.Sign()) == 1;

        /// <summary>
        /// If the two vectors are at 90 degrees
        /// </summary>
        /// <param name="cardinal1">First cardinal vector</param>
        /// <param name="cardinal2">Second cardinal vector</param>
        /// <param name="trustCardinality">If both vectors need verification to be cardinal vectors</param>
        /// <returns></returns>
        public static bool IsOrthogonalCardinal(this Vector2Int cardinal1, Vector2Int cardinal2, bool trustCardinality = true) =>
             cardinal1.x == 0 == (cardinal2.x != 0) && (trustCardinality || (cardinal1.IsCardinal() && cardinal2.IsCardinal()));

        /// <summary>
        /// If the two vectors are at 90 degrees
        /// </summary>
        /// <param name="cardinal1">First cardinal vector</param>
        /// <param name="cardinal2">Second cardinal vector</param>
        /// <param name="trustCardinality">If both vectors need verification to be cardinal vectors</param>
        /// <returns></returns>
        public static bool IsOrthogonalCardinal(this Vector3Int cardinal1, Vector3Int cardinal2, bool trustCardinality = true) =>
             (cardinal1.x == 0 == (cardinal2.x != 0) || cardinal1.y == 0 == (cardinal2.y != 0)) && (trustCardinality || (cardinal1.IsCardinal() && cardinal2.IsCardinal()));

        public static bool IsInverseDirection(this Vector2Int direction1, Vector2Int direction2) =>
            direction1.x.Sign() == -direction2.x.Sign()
            && direction1.y.Sign() == -direction2.y.Sign();

        public static bool IsInverseDirection(this Vector3Int direction1, Vector3Int direction2) =>
            direction1.x.Sign() == -direction2.x.Sign()
            && direction1.y.Sign() == -direction2.y.Sign()
            && direction1.z.Sign() == -direction2.z.Sign();

        #endregion Cardinality

        #region Rotations

        public static Quaternion AsQuaternion(this Vector2Int direction) =>
            Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));

        public static Quaternion AsQuaternion(this Vector3Int direction) =>
            Quaternion.LookRotation(new Vector3(direction.x, direction.y, direction.z));

        public static Quaternion AsQuaternion(this Vector3Int direction, Vector3Int down) =>
            Quaternion.LookRotation(new Vector3(direction.x, direction.y, direction.z), new Vector3(down.x, -down.y, down.z));

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
            direction.RotateCCW(up * -1);

        /// <summary>
        /// If the first vector is a clockwise rotation of the second
        /// </summary>
        public static bool IsCWRotationOf(this Vector2Int cardinal1, Vector2Int cardinal2) =>
            cardinal1 == cardinal2.RotateCW();

        /// <summary>
        /// If the first vector is a clockwise rotation of the second around the up axis
        /// </summary>
        public static bool IsCWRotationOf(this Vector3Int cardinal1, Vector3Int cardinal2, Vector3Int up) =>
            cardinal1 == cardinal2.RotateCW(up);

        /// <summary>
        /// If the first vector is a counter-clockwise rotation of the second
        /// </summary>
        public static bool IsCCWRotationOf(this Vector2Int cardinal1, Vector2Int cardinal2) =>
            cardinal1 == cardinal2.RotateCCW();

        /// <summary>
        /// If the first vector is a counter-clockwise rotation of the second around the up axis
        /// </summary>
        public static bool IsCCWRotationOf(this Vector3Int cardinal1, Vector3Int cardinal2, Vector3Int up) =>
            cardinal1 == cardinal2.RotateCCW(up);

        #endregion Rotations

        #region Distances

        /// <summary>
        /// Summation of distances along all axis
        /// </summary>
        public static int ManhattanDistance(this Vector2Int point, Vector2Int other) =>
                Mathf.Abs(point.x - other.x) + Mathf.Abs(point.y - other.y);

        /// <summary>
        /// Summation of distances along all axis
        /// </summary>
        public static int ManhattanDistance(this Vector3Int point, Vector3Int other) =>
                Mathf.Abs(point.x - other.x) + Mathf.Abs(point.y - other.y) + Mathf.Abs(point.z - other.z);

        /// <summary>
        /// Largest distance along any distance
        /// </summary>
        public static int ChebyshevDistance(this Vector2Int point, Vector2Int other) =>
            Mathf.Max(Mathf.Abs(point.x - other.x), Mathf.Abs(point.y - other.y));

        /// <summary>
        /// Largest distance along any distance
        /// </summary>
        public static int ChebyshevDistance(this Vector3Int point, Vector3Int other) =>
            Mathf.Max(Mathf.Abs(point.x - other.x), Mathf.Abs(point.y - other.y), Mathf.Abs(point.z - other.z));

        #endregion Distances

        #region Intersection

        /// <summary>
        /// Get the intersection point of two vectors
        /// </summary>
        /// <param name="point">Reference point</param>
        /// <param name="target">Ohter point</param>
        /// <param name="cardinalAxis">The cardinal axis from the reference point on which the intersection should lie</param>
        public static Vector2Int OrthoIntersection(this Vector2Int point, Vector2Int target, Vector2Int cardinalAxis)
        {
            var candidate = new Vector2Int(target.x, point.y);
            var direction = candidate - point;
            if (!direction.IsOrthogonalCardinal(cardinalAxis)) return candidate;

            return new Vector2Int(point.x, target.y);
        }

        #endregion Intersection

        #region World

        /// <summary>
        /// Returns float world position assuming the 2D int vector represents an XZ plane
        /// </summary>
        /// <param name="elevation">Elevation of the 2D in vector plane in int-space</param>
        /// <param name="scale">Size of each int space step in the world coordinates space</param>
        public static Vector3 ToPositionFromXZPlane(this Vector2Int coords, int elevation = 0, int scale = 3) =>
            new Vector3(coords.x * scale, elevation * scale, coords.y * scale);

        /// <summary>
        /// Returns float world position
        /// </summary>
        /// <param name="scale">Size of each int space step in the world coordinates space</param>
        public static Vector3 ToPosition(this Vector3Int coords, float scale = 3, bool invertZ = false) =>
            new Vector3(coords.x * scale, coords.y * scale, invertZ ? coords.z * -scale : coords.z * scale);

        public static Vector3 ToDirectionFromXZPlane(this Vector2Int direction) => new Vector3(direction.x, 0, direction.y);

        public static Vector3 ToDirection(this Vector3Int direction) => new Vector3(direction.x, direction.y, direction.z);
        public static Vector3 ToDirection(this Vector3Int direction, float scale) => new Vector3(direction.x * scale, direction.y * scale, direction.z * scale);

        #endregion World

        #region 2D/3D
        public static Vector3Int To3DFromXZPlane(this Vector2Int vector, int elevation = 0) => new Vector3Int(vector.x, elevation, vector.y);
        public static Vector2Int To2DInXZPlane(this Vector3Int vector) => new Vector2Int(vector.x, vector.z);
        public static Vector2Int To2DInXZPlane(this Vector3Int vector, out int elevation)
        {
            elevation = vector.y;
            return new Vector2Int(vector.x, vector.z);
        }
        #endregion

        #region Rect
        public static RectInt ToUnitRect(this Vector2Int vector) => new RectInt(vector, Vector2Int.one);
        #endregion Rect
    }
}