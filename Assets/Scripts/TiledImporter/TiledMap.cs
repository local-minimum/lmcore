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
        public List<TiledObjectLayer> ObjectLayers = new List<TiledObjectLayer>();
        public List<TiledGroup> Groups = new List<TiledGroup>();
        public TiledCustomProperties CustomProperties;

        public static TiledMap From(XElement map, TiledEnums enums, string source, bool filterImports, bool scaleCoordinates)
        {
            var tiledMap = CreateInstance<TiledMap>();
            tiledMap.Enums = enums;

            if (map == null) return tiledMap;

            tiledMap.Metadata = TiledMapMetadata.From(map, enums, source);

            var scaling = scaleCoordinates ? tiledMap.Metadata.TileSize : Vector2Int.zero;

            tiledMap.Tilesets = map.HydrateElementsByName(
                "tileset",
                TiledTilesetMetadata.From
            ).ToList();

            tiledMap.Layers = map.HydrateElementsByName(
                "layer", 
                TiledLayer.FromFactory(enums), 
                TiledLayer.ShouldBeImported(filterImports)
            ).ToList();

            tiledMap.ObjectLayers = map.HydrateElementsByName(
                "objectgroup",
                TiledObjectLayer.FromFactory(enums, scaling),
                TiledObjectLayer.ShouldBeImported(filterImports)
            ).ToList();

            tiledMap.Groups = map.HydrateElementsByName(
                "group",
                TiledGroup.FromFactory(enums, filterImports, scaling),
                TiledGroup.ShouldBeImported(filterImports)
            ).ToList();

            tiledMap.CustomProperties = TiledCustomProperties.From(map.Element("properties"), enums);

            return tiledMap;
        }

        public string LayerNames()
        {
            return string.Join(", ", string.Join(", ", Layers.Select(l => l.Name)), string.Join(", ", Groups.Select(group => group.LayerNames())));
        }

        private IEnumerable<TiledLayer> IterateLayers
        {
            get
            {
                foreach (var layer in Layers) yield return layer;
                foreach (var group in Groups)
                {
                    foreach (var layer in group.Layers) yield return layer;
                }
            }
        }

        public IEnumerable<TiledObjectLayer> IterateObjectLayers
        {
            get
            {
                foreach (var layer in ObjectLayers) yield return layer;
                foreach (var group in Groups)
                {
                    foreach (var layer in group.ObjectLayers) yield return layer;
                }
            }
        }

        public IEnumerable<T> FindInLayers<T>(Func<TiledLayer, T> action) => 
            IterateLayers.Select(action);

        public IEnumerable<TiledLayer> FindLayers(Func<TiledLayer, bool> filter) => 
            IterateLayers.Where(filter);

        public IEnumerable<TiledObjectLayer> FindObjectLayers(Func<TiledObjectLayer, bool> filer) =>
            IterateObjectLayers.Where(filer);

        public TiledTilesetMetadata GetTilesetMetadataForTileId(int tileId) =>
            Tilesets.OrderByDescending(t => t.FirstGID).FirstOrDefault(t => t.FirstGID < tileId);

        public TiledTile GetTile(int tileId, TiledTileset[] tilesets)
        {
            var metadata = GetTilesetMetadataForTileId(tileId);
            if (metadata == null) return null;
            var tilesetTileId = tileId - metadata.FirstGID;

            for (var i = 0; i < tilesets.Length; i++) { 
                var tileset = tilesets[i];
                if (tileset.Source.Contains(metadata.Source))
                {
                    var tile = tileset.Tiles.FirstOrDefault(t => t.Id == tilesetTileId);
                    if (tile == null)
                    {
                        Debug.LogWarning($"Could not locate ID {tileId} in {tileset.Source}");
                    }
                    return tile;
                }
            }

            Debug.LogWarning($"No tileset matched source {metadata.Source} ({string.Join(" | ", tilesets.Select(t => t.Source))})");
            return null;
        }
    }
}
