using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using LMCore.Extensions;

namespace TiledImporter
{

    [Serializable]
    public class TiledMap: ScriptableObject
    {
        public TiledEnums Enums;
        public TiledMapMetadata Metadata = new TiledMapMetadata();
        public List<TiledTilesetMetadata> Tilesets = new List<TiledTilesetMetadata>();
        public List<TiledLayer> Layers = new List<TiledLayer>();
        public List<TiledGroup> Groups = new List<TiledGroup>();
        public TiledCustomProperties CustomProperties;

        public static TiledMap From(XElement map, TiledEnums enums, string source, bool filterImports)
        {
            var tiledMap = CreateInstance<TiledMap>();
            tiledMap.Enums = enums;

            if (map == null) return tiledMap;

            tiledMap.Metadata = TiledMapMetadata.From(map, enums, source);

            tiledMap.Tilesets = map.HydrateElementsByName(
                "tileset",
                TiledTilesetMetadata.From
            ).ToList();

            tiledMap.Layers = map.HydrateElementsByName(
                "layer", 
                TiledLayer.FromFactory(enums), 
                TiledLayer.ShouldBeImported(filterImports)
            ).ToList();

            tiledMap.Groups = map.HydrateElementsByName(
                "group",
                TiledGroup.FromFactory(enums, filterImports),
                TiledGroup.ShouldBeImported(filterImports)
            ).ToList();

            tiledMap.CustomProperties = TiledCustomProperties.From(map.Element("properties"), enums);

            return tiledMap;
        }

        public string LayerNames()
        {
            return string.Join(", ", string.Join(", ", Layers.Select(l => l.Name)), string.Join(", ", Groups.Select(group => group.LayerNames())));
        }

    }
}
