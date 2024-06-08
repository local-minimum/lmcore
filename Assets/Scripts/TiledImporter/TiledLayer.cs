﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace TiledImporter
{
    [Serializable]
    public class TiledLayer
    {
        public string Name;
        public int Id;
        public Vector2Int LayerSize;
        public int[,] Tiles;
        public TiledCustomProperties CustomProperties;

        public static Func<TiledLayer, bool> ShouldBeImported(bool filterLayerImport) {
            return (TiledLayer layer) => !filterLayerImport || (layer?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false);
        }

        public static Func<XElement, TiledLayer> fromFactory(TiledEnums enums) { 
            return (XElement layer) => from(layer, enums);
        }
        public static TiledLayer from(XElement layer, TiledEnums enums)
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
                CustomProperties = TiledCustomProperties.from(layer.Element("properties"), enums),
            };
        }

        public string TilesAsASCII(int baseOrd = 64)
        {
            var builder = new StringBuilder();

            for (int row = 0; row < LayerSize.y; row++)
            {
                for (int col = 0; col < LayerSize.x; col++)
                {
                    var value = Tiles[row, col];
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
