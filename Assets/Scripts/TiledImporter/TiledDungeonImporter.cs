using LMCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor.AssetImporters;
using UnityEditor.Purchasing;
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
                    // This is just fake... should rewrite to not have to sum it all
                    .Sum())
            .Sum();


        return output;
    }
}


static class XElementExtensions
{
    public static string GetAttribute(this XElement element, string attributeName) =>
        element.Attribute(attributeName)?.Value;

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
    public class TiledEnum<T>
    {
        public string TypeName;
        public T Value;
    }

    [SerializeField]
    public class TiledEnums
    {
        public SerializableDictionary<string, SerializableDictionary<string, TiledEnum<string>>> StringEnums = new SerializableDictionary<string, SerializableDictionary<string, TiledEnum<string>>>(); 
        public SerializableDictionary<string, SerializableDictionary<int, TiledEnum<int>>> IntEnums = new SerializableDictionary<string, SerializableDictionary<int, TiledEnum<int>>>(); 

        public static bool IsEnumProperty(XElement property) => property.GetAttribute("propertytype") != null && property.Element("properties") == null;

        public static bool IsStringEnumProperty(XElement property) => IsEnumProperty(property) && property.GetAttribute("type") == "int";
        public static bool IsIntEnumProperty(XElement property) => IsEnumProperty(property) && (property.GetAttribute("type") == null || property.GetAttribute("type") == "string");

        public TiledEnum<int> parseIntEnum(XElement property)
        {
            var typeName = property.GetAttribute("propertytype");

            if (!IntEnums.ContainsKey(typeName))
            {
                IntEnums.Add(typeName, new SerializableDictionary<int, TiledEnum<int>>());
            }

            var intEnum = IntEnums[typeName];

            var intValue = property.GetIntAttribute("value");

            if (!intEnum.ContainsKey(intValue))
            {
                intEnum.Add(intValue, new () { TypeName = typeName, Value = intValue });
            }

            return intEnum[intValue];
        }

        public TiledEnum<string> parseStringEnum(XElement property)
        {
            var typeName = property.GetAttribute("propertytype");

            if (!StringEnums.ContainsKey(typeName))
            {
                StringEnums.Add(typeName, new SerializableDictionary<string, TiledEnum<string>>());
            }

            var stringEnum = StringEnums[typeName];

            var stringValue = property.GetAttribute("value");

            if (!stringEnum.ContainsKey(stringValue))
            {
                stringEnum.Add(stringValue, new TiledEnum<string>() { TypeName = typeName, Value = stringValue });
            }
            return stringEnum[stringValue];
        }
    }


    [SerializeField]
    public class CustomProperties
    {
        // Not Supported: file, object, enums
        public SerializableDictionary<string, string> Strings;
        public SerializableDictionary<string, int> Ints;
        public SerializableDictionary<string, float> Floats;
        public SerializableDictionary<string, bool> Bools;
        public SerializableDictionary<string, Color> Colors;
        public SerializableDictionary<string, CustomProperties> Classes;
        public SerializableDictionary<string, TiledEnum<string>> StringEnums;
        public SerializableDictionary<string, TiledEnum<int>> IntEnums;

        private static IEnumerable<XElement> FilterByNonEnumType(
            XElement properties, 
            string type, 
            bool includeMissingAttribute
        ) =>
            properties.Elements().Where(element => 
                !TiledEnums.IsEnumProperty(element) && 
                (element.GetAttribute("type") == type || includeMissingAttribute && element.GetAttribute("type") == null)
            );

        private static T Parse<T>(XElement property, TiledEnums enums)
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

            if (t == typeof(CustomProperties))
            {
                return (T) Convert.ChangeType(from(property.Element("properties"), enums), t);
            }

            throw new NotImplementedException($"Custom properties of type {t} not supported");
        }

        private static SerializableDictionary<string, T> FromFilter<T>(
            XElement properties, 
            string type,
            TiledEnums enums,
            bool includeMissingType = false
        ) => new SerializableDictionary<string, T>(
                FilterByNonEnumType(properties, type, includeMissingType)
                    .Select(property => new KeyValuePair<string, T>(property.GetAttribute("name"), Parse<T>(property, enums)))
            );

        public static CustomProperties from(XElement properties, TiledEnums enums) => properties == null ? null : new CustomProperties()
        {
            Strings = FromFilter<string>(properties, "string", enums, true),
            Ints = FromFilter<int>(properties, "int", enums),
            Floats = FromFilter<float>(properties, "float", enums),
            Bools = FromFilter<bool>(properties, "bool", enums),
            Colors = FromFilter<Color>(properties, "color", enums),
            StringEnums = new SerializableDictionary<string, TiledEnum<string>>(
                properties
                    .Elements()
                    .Where(property => TiledEnums.IsStringEnumProperty(property))
                    .Select(property => new KeyValuePair<string, TiledEnum<string>>(property.GetAttribute("name"), enums.parseStringEnum(property)))
            ),
            IntEnums = new SerializableDictionary<string, TiledEnum<int>>(
                properties
                    .Elements()
                    .Where(property => TiledEnums.IsIntEnumProperty(property))
                    .Select(property => new KeyValuePair<string, TiledEnum<int>>(property.GetAttribute("name"), enums.parseIntEnum(property)))
            )
        };
    }

    [Serializable]
    public class MapMetadata
    {
        public Vector2Int MapSize;
        public bool Infinite;
        public CustomProperties CustomProperties;

        public override string ToString() => $"<MapMetadata size={MapSize} infinite={Infinite} />";

        public static MapMetadata from(XElement map, TiledEnums enums) => map == null ? null :
            new MapMetadata() {
                MapSize = map.GetVector2IntAttribute("width", "height"),
                Infinite = map.GetBoolAttribute("infinite"),
                CustomProperties = CustomProperties.from(map.Element("properties"), enums),
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

        public static TileLayer from(XElement layer, TiledEnums enums)
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
                CustomProperties = CustomProperties.from(layer.Element("properties"), enums),
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

        public static LayerGroup from(XElement group, TiledEnums enums, bool filterImport) => group == null ? null : new LayerGroup()
        {
            Id = group.GetIntAttribute("id"),
            Name = group.GetAttribute("name"),
            Layers = group.Elements()
                .Where(element => element.Name == "layer")
                .Select(layer => TileLayer.from(layer, enums))
                .Where(layer => !filterImport || (layer?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false))
                .ToList(),
            CustomProperties = CustomProperties.from(group.Element("properties"), enums),
        };
    }


    [Serializable]
    public class TiledMap
    {
        public TiledEnums Enums;
        public MapMetadata Metadata;
        public List<TilesetMetadata> Tilesets;
        public List<TileLayer> Layers;
        public List<LayerGroup> Groups;

        public static TiledMap from(XElement map, TiledEnums enums, bool filterLayerImport)
        {
            if (map == null) return new TiledMap() { 
                Enums = enums, 
                Metadata = new MapMetadata(), 
                Tilesets = new List<TilesetMetadata>(), 
                Layers = new List<TileLayer>(), 
                Groups = new List<LayerGroup>() 
            };

            return  new TiledMap()
            {
                Enums = enums,
                Metadata = MapMetadata.from(map, enums),
                Tilesets = map
                .Elements()
                .Where(element => element.Name == "tileset")
                .Select(tileset => TilesetMetadata.from(tileset))
                .ToList(),
                Layers = map
                .Elements()
                .Where(element => element.Name == "layer")
                .Select(layer => TileLayer.from(layer, enums))
                .Where(layer => !filterLayerImport || (layer?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false))
                .ToList(),
                Groups = map
                .Elements()
                .Where(element => element.Name == "group")
                .Select(group => LayerGroup.from(group, enums, filterLayerImport))
                .ToList(),
            };
        }

        public string LayerNames()
        {
            return string.Join(", ", string.Join(", ", Layers.Select(l => l.Name)), string.Join(", ", Groups.Select(group => group.LayerNames())));
        }

    }

    void ProcessMap(XElement map)
    {
        var enums = new TiledEnums();
        var processedMap = TiledMap.from(map, enums, onlyImportFlaggedLayers);
        Debug.Log($"RootLayers: {processedMap.Layers.Count}");
        Debug.Log($"Groups: {processedMap.Groups.Count}");
        Debug.Log($"Layers: {processedMap.LayerNames()}");

        var layer = TileLayer.from(map.Element("group").Element("layer"), enums);
        Debug.Log(layer.TilesAsASCII());
    }
}
