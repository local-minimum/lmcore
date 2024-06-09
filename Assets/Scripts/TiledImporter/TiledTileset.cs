using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using LMCore.Extensions;

namespace TiledImporter
{

    public class TiledTileset: ScriptableObject
    {
        public string Source;
        public Vector2Int TileSize;
        public int Columns;
        public int TileCount;
        public string SourceImage;
        public Vector2Int SourceImageSize;
        public TiledEnums Enums;
        public List<TiledTile> Tiles = new List<TiledTile>();

        public string Name => $"TiledMap - {Path.GetFileNameWithoutExtension(Source)}";

        public static TiledTileset From(XElement tileset, TiledEnums enums, string source)
        {
            var tiledTileset = CreateInstance<TiledTileset>();

            tiledTileset.Enums = enums;
            tiledTileset.Source = source;

            if (tiledTileset == null) return tiledTileset;

            tiledTileset.TileSize = tileset.GetVector2IntAttribute("tilewidth", "tileheight");

            tiledTileset.Columns = tileset.GetIntAttribute("columns");
            tiledTileset.TileCount = tileset.GetIntAttribute("tilecount");

            tiledTileset.Tiles = tileset
                .HydrateElementsByName("tile", TiledTile.FromFactory(enums))
                .ToList();

            var image = tileset.Element("image");

            if (image == null) return tiledTileset;

            tiledTileset.SourceImage = image.GetAttribute("source");
            tiledTileset.SourceImageSize = image.GetVector2IntAttribute("width", "height");

            return tiledTileset;
        }
    }
}
