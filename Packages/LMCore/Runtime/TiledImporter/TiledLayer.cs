using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using LMCore.Extensions;

namespace LMCore.TiledImporter
{
    [Serializable]
    public class TiledLayer
    {
        public string Name;
        public int Id;
        public Vector2Int LayerSize;
        [SerializeField, HideInInspector]
        private int[] Tiles;
        public TiledCustomProperties CustomProperties;

        public static Func<TiledLayer, bool> ShouldBeImported(bool filterLayerImport) {
            return (TiledLayer layer) => !filterLayerImport || (layer?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false);
        }

        public static Func<XElement, TiledLayer> FromFactory(TiledEnums enums) { 
            return (XElement layer) => From(layer, enums);
        }

        public static TiledLayer From(XElement layer, TiledEnums enums)
        {
            if (layer == null) return null;

            var layerSize = layer.GetVector2IntAttribute("width", "height");

            var data = layer.Element("data");
            if (data.GetAttribute("encoding") != "csv")
            {
                throw new NotSupportedException("Only csv import supported");
            }

            return new TiledLayer()
            {
                Id = layer.GetIntAttribute("id"),
                Name = layer.GetAttribute("name"),
                LayerSize = layerSize,
                Tiles = IntCSVParserUtil.Parse((string)data, layerSize),
                CustomProperties = TiledCustomProperties.From(layer.Element("properties"), enums),
            };
        }


        public int this[int row, int col] => Tiles[row * LayerSize.x + col];
        public int this[Vector2Int coords] => Tiles[coords.y * LayerSize.x + coords.x];

        public bool InsideLayer(int row, int col) => row >= 0 && col >= 0 && row < LayerSize.y && col < LayerSize.x;
        public bool InsideLayer(Vector2Int coords) => coords.x >= 0 && coords.y >= 0 && coords.x < LayerSize.x && coords.y < LayerSize.y;

        public string TilesAsASCII(int baseOrd = 64)
        {
            var builder = new StringBuilder();

            for (int row = 0; row < LayerSize.y; row++)
            {
                for (int col = 0; col < LayerSize.x; col++)
                {
                    var value = this[row, col];
                    if (baseOrd > 32 && value == 0)
                    {
                        builder.Append(" ");
                    }
                    else
                    {
                        builder.Append((char)baseOrd + value);
                    }
                }

                builder.Append("\n");
            }

            return builder.ToString();
        }
    }
}
