using LMCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;


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

    public static float GetFloatAttribute(this XElement element, string attributeName) => 
        float.Parse(element.GetAttribute(attributeName));

    public static Color GetColorAttribute(this XElement element, string attributeName)
    {
        if (ColorUtility.TryParseHtmlString(element.GetAttribute(attributeName), out var color))
        {
            return color;
        }
        return Color.magenta;
    }

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
/// A bit inspired by SuperTiled2Unity https://github.com/Seanba/SuperTiled2Unity/
/// </summary>
[ScriptedImporter(1, new[] {"tmx"})]
public class TiledDungeonImporter : ScriptedImporter 
{
    [SerializeField, Range(0, 10)]
    private float tileSize = 3;

    [SerializeField, Tooltip("If only those levels with the custom boolean property 'Imported' set to true are imported")]
    bool onlyImportFlaggedLayers = true;

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

    [SerializeField]
    public class CustomProperties
    {
        // Not Supported: file, object or class
        public SerializableDictionary<string, string> Strings;
        public SerializableDictionary<string, int> Ints;
        public SerializableDictionary<string, float> Floats;
        public SerializableDictionary<string, bool> Bools;
        public SerializableDictionary<string, Color> Colors;

        private static IEnumerable<XElement> FilterByType(XElement properties, string type) =>
            properties.Elements().Where(element => element.GetAttribute("type") == type);

        private static T Parse<T>(XElement property)
        {
            var t = typeof(T);

            if (t == typeof(string))
            {
                return (T)Convert.ChangeType(property.GetAttribute("value"), t);
            }

            if (t == typeof(int))
            {
                return (T)Convert.ChangeType(property.GetIntAttribute("value"), t);
            }

            if (t == typeof(float))
            {
                return (T)Convert.ChangeType(property.GetFloatAttribute("value"), t);
            }

            if (t == typeof(bool))
            {
                return (T)Convert.ChangeType(property.GetBoolAttribute("value"), t);
            }

            if (t == typeof(Color))
            {
                return (T)Convert.ChangeType(property.GetBoolAttribute("value"), t);
            }

            throw new NotImplementedException($"Custom properties of type {t} not supported");
        }

        private static SerializableDictionary<string, T> FromFilter<T>(XElement properties, string type) => new SerializableDictionary<string, T>(
                FilterByType(properties, type)
                    .Select(property => new KeyValuePair<string, T>(property.GetAttribute("name"), Parse<T>(property)))
            );

        public static CustomProperties from(XElement properties) => properties == null ? null : new CustomProperties()
        {
            Strings = FromFilter<string>(properties, "string"),
            Ints = FromFilter<int>(properties, "int"),
            Floats = FromFilter<float>(properties, "float"),
            Bools = FromFilter<bool>(properties, "bool"),
            Colors = FromFilter<Color>(properties, "color"),
        };
    }

    [Serializable]
    public class MapMetadata
    {
        public Vector2Int MapSize;
        public bool Infinite;
        public CustomProperties CustomProperties;

        public override string ToString() => $"<MapMetadata size={MapSize} infinite={Infinite} />";

        public static MapMetadata from(XElement map) => map == null ? null :
            new MapMetadata() {
                MapSize = map.GetVector2IntAttribute("width", "height"),
                Infinite = map.GetBoolAttribute("infinite"),
                CustomProperties = CustomProperties.from(map.Element("properties")),
            };
    }

    [Serializable]
    public class TilesetMetadata
    {
        public int FirstGID;
        public string Source;

        public override string ToString() => $"<TilesetMetadata firstGID={FirstGID} source=\"{Source}\" />";

        public static TilesetMetadata from(XElement tileset) => tileset == null ? null :
            new TilesetMetadata() { FirstGID = tileset.GetIntAttribute("firstgid"), Source = tileset.GetAttribute("source") };
    }

    [Serializable]
    public class TileLayer
    {
        public int Id;
        public string Name;
        public Vector2Int LayerSize;
        public int[,] Tiles;
        public CustomProperties CustomProperties;

        public static TileLayer from(XElement layer)
        {
            if (layer == null) return null;

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
                CustomProperties = CustomProperties.from(layer.Element("properties")),
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
        public CustomProperties CustomProperties;

        public string LayerNames() => string.Join(", ", Layers.Select(layer => layer.Name));

        public static LayerGroup from(XElement group, bool filterImport) => group == null ? null : new LayerGroup()
        {
            Id = group.GetIntAttribute("id"),
            Name = group.GetAttribute("name"),
            Layers = group.Elements()
                .Where(element => element.Name == "layer")
                .Select(layer => TileLayer.from(layer))
                .Where(layer => !filterImport || (layer?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false))
                .ToList(),
            CustomProperties = CustomProperties.from(group.Element("properties")),
        };
    }


    [Serializable]
    public class TiledMap
    {
        public MapMetadata Metadata;
        public List<TilesetMetadata> Tilesets;
        public List<TileLayer> Layers;
        public List<LayerGroup> Groups;

        public static TiledMap from(XElement map, bool filterLayerImport) => map == null ? null : new TiledMap()
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
                .Where(layer => !filterLayerImport || (layer?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false))
                .ToList(),
            Groups = map
                .Elements()
                .Where(element => element.Name == "group")
                .Select(group => LayerGroup.from(group, filterLayerImport))
                .ToList(),
        };

        public string LayerNames()
        {
            return string.Join(", ", string.Join(", ", Layers.Select(l => l.Name)), string.Join(", ", Groups.Select(group => group.LayerNames())));
        }

    }

    void ProcessMap(XElement map)
    {

        var processedMap = TiledMap.from(map, onlyImportFlaggedLayers);
        Debug.Log($"RootLayers: {processedMap.Layers.Count}");
        Debug.Log($"Groups: {processedMap.Groups.Count}");
        Debug.Log($"Layers: {processedMap.LayerNames()}");

        var layer = TileLayer.from(map.Element("group").Element("layer"));
        Debug.Log(layer.TilesAsASCII());
    }
}
