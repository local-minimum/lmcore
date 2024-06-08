using UnityEngine;

namespace TiledImporter
{
    static class IntCSVParserUtil
    {
        public static int[,] Parse(string text, Vector2Int size)
        {
            var output = new int[size.y, size.x];

            var rowIdx = 0;
            foreach (var row in text.Trim().Split("\n"))
            {
                int colIdx = 0;

                foreach (var value in row.Trim().Split(","))
                {
                    if (rowIdx < size.y && colIdx < size.x)
                    {
                        if (int.TryParse(value.Trim(), out int intValue))
                        {
                            output[rowIdx, colIdx] = intValue;
                        }
                        else
                        {
                            Debug.LogWarning($"Parser encountered unexpected int value '{value}' ({rowIdx}, {colIdx})");
                        }
                    }
                    colIdx++;
                }

                rowIdx++;
            }

            return output;
        }
    }
}
