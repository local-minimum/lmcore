using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : Graphic
    {
        [SerializeField]
        float thickness = 1;

        [SerializeField]
        List<RectTransform> points = new List<RectTransform>();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var n = points.Count;

            if (n < 2) return;

            float previousAngle = 0f;
            for (int i = 0; i < n - 1; i++)
            {
                previousAngle = DrawLine(
                    vh,
                    transform.InverseTransformPoint(points[i].position),
                    transform.InverseTransformPoint(points[i + 1].position),
                    transform.InverseTransformPoint(points[Mathf.Min(i + 2, n - 1)].position),
                    previousAngle,
                    i);
            }
        }

        // Returns the z-component of the cross product of a and b
        static float CrossProductZ(Vector3 a, Vector3 b) =>
            (a.x * b.y) - (a.y * b.x);

        static float Orientation(Vector3 a, Vector3 b, Vector3 c)
        {
            return CrossProductZ(a, b) + CrossProductZ(b, c) + CrossProductZ(c, a);
        }

        static IEnumerable<Vector3> Sort4PointsClockwise(List<Vector3> points)
        {
            var a = points[0];
            var b = points[1];
            var c = points[2];
            var d = points[3];

            if (Orientation(a, b, c) < 0.0)
            {
                // Triangle abc is already clockwise.  Where does d fit?
                if (Orientation(a, c, d) < 0.0)
                {
                    yield return a;
                    yield return b;
                    yield return c;
                    yield return d;
                }
                else if (Orientation(a, b, d) < 0.0)
                {
                    yield return a;
                    yield return b;
                    yield return d;
                    yield return c;
                }
                else
                {
                    yield return d;
                    yield return b;
                    yield return d;
                    yield return a;
                }
            }
            else if (Orientation(a, c, d) < 0.0)
            {
                // Triangle abc is counterclockwise, i.e. acb is clockwise.
                // Also, acd is clockwise.
                if (Orientation(a, b, d) < 0.0)
                {
                    yield return a;
                    yield return c;
                    yield return b;
                    yield return d;
                }
                else
                {
                    yield return b;
                    yield return a;
                    yield return c;
                    yield return d;
                }
            }
            else
            {
                // Triangle abc is counterclockwise, and acd is counterclockwise.
                // Therefore, abcd is counterclockwise.
                yield return c;
                yield return b;
                yield return a;
                yield return d;
            }
        }

        float DrawLine(VertexHelper vh, Vector2 from, Vector2 to, Vector2 next, float inAngle, int segment)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            var d1 = to - from;
            var d2 = next - to;

            // We use prevously calculated in angle
            var a1 = segment == 0 ? Mathf.Atan2(d1.y, d1.x) : inAngle;
            // We dont' need to bias second angle with next if we're last
            var a2 = to == next ? Mathf.Atan2(d1.y, d1.x) : (Mathf.Atan2(d1.y, d2.x) + Mathf.Atan2(d2.y, d2.x)) / 2f;

            var firstOrthoAngle = a1 + (Mathf.PI / 2f);
            var secondOrthoAngle = a2 + (Mathf.PI / 2f);

            var firstOffset = Quaternion.Euler(0, 0, firstOrthoAngle * Mathf.Rad2Deg) * new Vector3(thickness / 2f, 0);
            var secondOffset = Quaternion.Euler(0, 0, secondOrthoAngle * Mathf.Rad2Deg) * new Vector3(thickness / 2f, 0);

            foreach (var coord in Sort4PointsClockwise(new List<Vector3> {

                (Vector3)from - firstOffset,
                (Vector3)from + firstOffset,
                (Vector3)to - secondOffset,
                (Vector3)to + secondOffset,
            }))
            {
                vertex.position = coord;
                vh.AddVert(vertex);
            }

            int offset = segment * 4;
            vh.AddTriangle(offset, offset + 1, offset + 2);
            vh.AddTriangle(offset, offset + 2, offset + 3);

            return a2;
        }
    }
}
