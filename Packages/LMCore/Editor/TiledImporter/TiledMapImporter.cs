using System;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;
using LMCore.Extensions;

namespace LMCore.TiledImporter
{
    /// <summary>
    /// A bit inspired by SuperTiled2Unity https://github.com/Seanba/SuperTiled2Unity/
    /// </summary>
    [ScriptedImporter(1, new[] { "tmx" })]
    public partial class TiledMapImporter : ScriptedImporter
    {
        [SerializeField, Tooltip("If only those levels with the custom boolean property 'Imported' set to true are imported")]
        bool onlyImportFlaggedLayers = true;

        [SerializeField, Tooltip("If the dimensions and coordinates of objects should be scaled down by the tile size")]
        bool scaleObjectCoordinates = false;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            XDocument doc = XDocument.Load(assetPath);
            if (doc != null)
            {
                // Early out if Zstd compression is used. This simply isn't supported by Unity.
                if (doc.Descendants("data").Where(x => x.GetAttribute("compression") == "zstd").Count() > 0)
                {
                    throw new NotSupportedException("Unity does not support Zstandard compression.\nSelect a different 'Tile Layer Format' in your map settings in Tiled and resave.");
                } else if (doc.Descendants("data").Where(x => !x.AttributeIsIfExists("compression","csv")).Count() > 0)
                {
                    throw new NotSupportedException("Importer only supports csv encoded data at the moment");
                }

                var map = ProcessMap(doc.Element("map"));
                Debug.Log($"{map.Metadata.Name} imported");

                ctx.AddObjectToAsset(map.Metadata.Name, map);
            }
        }

        TiledMap ProcessMap(XElement map)
        {
            var enums = new TiledEnums();
            var processedMap = TiledMap.From(map, enums, assetPath, onlyImportFlaggedLayers, scaleObjectCoordinates);
            Debug.Log($"RootLayers: {processedMap.Layers.Count}");
            Debug.Log($"Groups: {processedMap.Groups.Count}");
            Debug.Log($"Layers: {processedMap.LayerNames()}");

            return processedMap;
        }
    }
}
