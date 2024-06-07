using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Tilemaps;


static class IntCSVParserUtil
{
    public static int[,] Parse(string text, Vector2Int size)
    {
        var output = new int[size.y, size.x];

        var elems = text
            .Split("\n")
            .Select((row, rowIdx) =>
                row
                    .Trim()
                    .Split(",")
                    .Select((value, colIdx) =>
                    {
                        if (rowIdx < size.y && colIdx < size.x)
                        {
                            var trimmedValue = value.Trim();
                            if (int.TryParse(trimmedValue, out int intValue))
                            {
                                output[rowIdx, colIdx] = intValue;
                            } else
                            {
                                Debug.LogWarning($"Parser encountered unexpected int value '{value}'");
                            }
                        }

                        return 1;
                    })
                    // This is just fake...
                    .Sum())
            .Sum();


        return output;
    }
}


static class XElementExtensions
{
    public static string GetAttribute(this XElement element, string attributeName) =>
        element.Attribute(attributeName).Value;

    public static int GetIntAttribute(this XElement element, string attributeName) => 
        int.Parse(element.GetAttribute(attributeName));

    public static bool GetBoolAttribute(this XElement element, string attributeName)
    {
        switch (element.GetAttribute(attributeName).Trim().ToLower())
        {
            case "":
            case "f":
            case "0":
            case "false": 
                return false;
            default: 
                return true;
        }
    }

    public static Vector2Int GetVector2IntAttribute(this XElement element, string xAttributeName, string yAttributeName) => 
        new Vector2Int(element.GetIntAttribute(xAttributeName), element.GetIntAttribute(yAttributeName));
}

/// <summary>
/// Heavily inspired by SuperTiled2Unity https://github.com/Seanba/SuperTiled2Unity/
/// </summary>
[ScriptedImporter(1, new[] {"tmx"})]
public class TiledDungeonImporter : ScriptedImporter 
{
    [SerializeField, Range(0, 10)]
    private float tileSize = 3;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        XDocument doc = XDocument.Load(assetPath);
        if (doc != null)
        {
            // Early out if Zstd compression is used. This simply isn't supported by Unity.
            if (doc.Descendants("data").Where(x => ((string)x.Attribute("compression")) == "zstd").Count() > 0)
            {
                throw new NotSupportedException("Unity does not support Zstandard compression.\nSelect a different 'Tile Layer Format' in your map settings in Tiled and resave.");
            }

            ProcessMap(doc.Element("map"));
        }
    }

    [Serializable]
    public class MapMetadata
    {
        public Vector2Int MapSize;
        public bool Infinite;

        public override string ToString() => $"<MapMetadata size={MapSize} infinite={Infinite} />";

        public static MapMetadata from(XElement map) =>
            new MapMetadata() { MapSize = map.GetVector2IntAttribute("width", "height"), Infinite = map.GetBoolAttribute("infinite") };
    }


    [Serializable]
    public class TilesetMetadata
    {
        public int FirstGID;
        public string Source;

        public override string ToString() => $"<TilesetMetadata firstGID={FirstGID} source=\"{Source}\" />";

        public static TilesetMetadata from(XElement tileset) =>
            new TilesetMetadata() { FirstGID = tileset.GetIntAttribute("firstgid"), Source = tileset.GetAttribute("source") };
    }

    [Serializable]
    public class TileLayer
    {
        public int Id;
        public string Name;
        public Vector2Int LayerSize;
        public int[,] Tiles;

        public static TileLayer from(XElement layer)
        {
            var layerSize = layer.GetVector2IntAttribute("width", "height");

            var data = layer.Element("data");
            if (data.GetAttribute("encoding") != "csv")
            {
                throw new NotSupportedException("Only csv import supported");
            }

            return new TileLayer() {
                Id = layer.GetIntAttribute("id"),
                Name = layer.GetAttribute("name"),
                LayerSize = layerSize,
                Tiles = IntCSVParserUtil.Parse((string)data, layerSize),
            };
        }

        public string TilesAsASCII(int baseOrd = 64) {
            var builder = new StringBuilder();

            for (int row = 0; row < LayerSize.y; row++)
            {
                for (int col = 0; col < LayerSize.x; col++)
                {
                    var value = Tiles[row, col];
                    if (baseOrd > 32 && value == 0)
                    {
                        builder.Append(" ");
                    } else
                    {
                        builder.Append((char) baseOrd + value); 
                    }
                }

                builder.Append("\n");
            }

            return builder.ToString();
        }
    }

    [Serializable]
    public class LayerGroup
    {
        public int Id;
        public string Name;
        public List<TileLayer> Layers;

        public static LayerGroup from(XElement group) => new LayerGroup()
        {
            Id = group.GetIntAttribute("id"),
            Name = group.GetAttribute("name"),
            Layers = group.Elements().Where(element => element.Name == "layer").Select(layer => TileLayer.from(layer)).ToList()
        };
    }


    [Serializable]
    public class TiledMap
    {
        public MapMetadata Metadata;
        public List<TilesetMetadata> Tilesets;
        public List<TileLayer> Layers;
        public List<LayerGroup> Groups;

        public static TiledMap from(XElement map) => new TiledMap()
        {
            Metadata = MapMetadata.from(map),
            Tilesets = map
                .Elements()
                .Where(element => element.Name == "tileset")
                .Select(tileset => TilesetMetadata.from(tileset))
                .ToList(),
            Layers = map
                .Elements()
                .Where(element => element.Name == "layer")
                .Select(layer => TileLayer.from(layer))
                .ToList(),
            Groups = map
                .Elements()
                .Where(element => element.Name == "group")
                .Select(group => LayerGroup.from(group))
                .ToList(),
        };
    }

    void ProcessMap(XElement map)
    {

        var processedMap = TiledMap.from(map);
        Debug.Log($"RootLayers: {processedMap.Layers.Count}");
        Debug.Log($"Groups: {processedMap.Groups.Count}");

        var layer = TileLayer.from(map.Element("group").Element("layer"));
        Debug.Log(layer.TilesAsASCII());
    }
}
