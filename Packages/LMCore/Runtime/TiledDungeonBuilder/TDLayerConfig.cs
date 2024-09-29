using LMCore.TiledImporter;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LMCore.Extensions;

namespace LMCore.TiledDungeon
{
    public class TDLayerConfig
    {
        public bool TopLayer { get; private set; }  

        public int Elevation {  get; private set; } 
        public TiledLayer LayoutLayer { get; private set; }
        public List<TiledLayer> ModificationLayers { get; private set; }

        public Vector2Int LayerSize { get; private set; }

        public List<TiledObjectLayer> ObjectLayers { get; private set; }

        private TiledMap _Map { get; set; }
        private TiledTileset[] _Tilesets { get; set; }

        public TDLayerConfig(TiledMap map, TiledTileset[] tilesets, int elevation, bool topLayer) { 
            _Map = map;
            _Tilesets = tilesets;
            Elevation = elevation;
            TopLayer = topLayer;

            var layers = map
                .FindLayers(layer => layer.CustomProperties.Int(TiledConfiguration.instance.LayerElevationKey) == elevation)
                .ToList();

            ObjectLayers = map
                .FindObjectLayers(layer => layer.CustomProperties.Int(TiledConfiguration.instance.LayerElevationKey) == elevation)
                .ToList();

            LayoutLayer = layers.FirstOrDefault(l => l.Name.StartsWith(TiledConfiguration.instance.LayoutLayerPrefix));

            if (LayoutLayer == null)
            {
                Debug.LogError(
                    $"No layout layer found for elevation {elevation} ({string.Join(", ", layers.Select(l => l.Name))})"
                );

                return ;
            }

            LayerSize = LayoutLayer.LayerSize;

            ModificationLayers = layers.Where(l => l != LayoutLayer).ToList();
        
        }

        public Vector3Int AsUnityCoordinates(int col, int row) =>
            new Vector3Int(col, Elevation, LayerSize.y - row - 1);

        public Vector2Int InverseCoordinates(Vector2Int coordinates) { 
            return new Vector2Int(coordinates.x, LayerSize.y - coordinates.y - 1); 
        }

        public IEnumerable<TileModification> getModifications(Vector3Int coords) =>
            coords.y == Elevation ? getModsTiledCoords(LayerSize.y - coords.z - 1, coords.x) : null;

        IEnumerable<TileModification> getModsTiledCoords(int row, int col) =>
            ModificationLayers
                .Select(modLayer =>
                {
                    if (!modLayer.InsideLayer(row, col)) return null;

                    var modId = modLayer[row, col];
                    var tile = _Map.GetTile(modId, _Tilesets);

                    if (tile == null) return null;

                    return new TileModification()
                    {
                        Layer = modLayer.Name,
                        LayerProperties = modLayer.CustomProperties,
                        Tile = tile
                    };
                })
                .Where(tm => tm != null);

        public TiledTile GetTile(Vector3Int coordinates)
        {
            var tiledCoordinates = InverseCoordinates(coordinates.To2DInXZPlane());
            var tileId = LayoutLayer[tiledCoordinates.y, tiledCoordinates.x];

            var tile = _Map.GetTile(tileId, _Tilesets);

            if (tile == null)
            {
                if (tileId > 0) Debug.LogWarning($"Dungeon: Tile Id {tileId} @ Row({tiledCoordinates.y}) Col({tiledCoordinates.x}) Elevation({coordinates.y}) not known by map");
                return null;
            }

            return tile;
        }
    }
}
