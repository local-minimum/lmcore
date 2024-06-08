using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TiledImporter
{
    static class IntCSVParserUtil
    {
        public static int[,] Parse(string text, Vector2Int size)
        {
            var output = new int[size.y, size.x];

            foreach (
                var (rowIdx, row)
                in text
                    .Split("\n")
                    .Select((row, rowIdx) =>
                        new KeyValuePair<int, IEnumerable<string>>(
                            rowIdx,
                            row.Trim().Split(",")
                    ))
            )
            {
                int colIdx = 0;
                foreach (var value in row)
                {
                    colIdx++;
                    if (rowIdx < size.y && colIdx < size.x)
                    {
                        if (int.TryParse(value.Trim(), out int intValue))
                        {
                            output[rowIdx, colIdx] = intValue;
                        }
                        else
                        {
                            Debug.LogWarning($"Parser encountered unexpected int value '{value}'");
                        }
                    }
                }
            }

            return output;
        }
    }
}
