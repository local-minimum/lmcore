using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIGridRenderer : Graphic
    {
        [SerializeField, Range(0, 100)]
        float lineThickness = 1f;

        [SerializeField]
        Vector2Int gridSize = new Vector2Int(4, 4);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float cellWidth = rectTransform.rect.width / gridSize.x;
            float cellHeight = rectTransform.rect.height / gridSize.y;

            var min = rectTransform.rect.min;

            int cell = 0;
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {

                    DrawCell(
                        vh,
                        min + new Vector2(x * cellWidth, y * cellHeight),
                        min + new Vector2((x + 1) * cellWidth, (y + 1) * cellHeight),
                        cell);
                    cell++;
                }
            }


        }

        void DrawCell(VertexHelper vh, Vector2 min, Vector2 max, int index)
        {

            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            // #0 Lower Left
            vertex.position = min;
            vh.AddVert(vertex);

            // #1 Upper Left
            vertex.position = new Vector3(min.x, max.y);
            vh.AddVert(vertex);

            // #2 Upper Right
            vertex.position = max;
            vh.AddVert(vertex);

            // #3 Lower Right
            vertex.position = new Vector3(max.x, min.y);
            vh.AddVert(vertex);

            // #4 Lower Left inner
            vertex.position = new Vector3(min.x + lineThickness, min.y + lineThickness);
            vh.AddVert(vertex);

            // #5 Upper Left inner
            vertex.position = new Vector3(min.x + lineThickness, max.y - lineThickness);
            vh.AddVert(vertex);

            // #6 Upper Right inner
            vertex.position = new Vector3(max.x - lineThickness, max.y - lineThickness);
            vh.AddVert(vertex);

            // #6 Lower Right inner
            vertex.position = new Vector3(max.x - lineThickness, min.y + lineThickness);
            vh.AddVert(vertex);

            var start = index * 8;
            vh.AddTriangle(start, start + 1, start + 4);
            vh.AddTriangle(start + 4, start + 1, start + 5);

            vh.AddTriangle(start + 1, start + 2, start + 5);
            vh.AddTriangle(start + 5, start + 2, start + 6);

            vh.AddTriangle(start + 2, start + 3, start + 6);
            vh.AddTriangle(start + 6, start + 3, start + 7);

            vh.AddTriangle(start + 3, start + 0, start + 7);
            vh.AddTriangle(start + 7, start + 0, start + 4);
        }
    }
}
