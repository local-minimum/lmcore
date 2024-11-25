using UnityEngine;
using LMCore.Inventory;

namespace LMCore.TiledDungeon
{
    public static class TDDungeonGenerator
    {
        private static void GenerateNode(TiledDungeon dungeon, Vector3Int coordinates, Transform parent)
        {

            var layerConfig = dungeon.GetLayerConfig(coordinates.y);
            var tile = layerConfig.GetTile(coordinates);

            if (tile == null) return;

            // Shouldn't create same twice, no reason for this to happen though...
            if (dungeon.HasNodeAt(coordinates)) return;


            var node = GameObject.Instantiate(dungeon.Prefab, parent);

            node.Coordinates = coordinates;
            dungeon[coordinates] = node;

            var nodeConfig = dungeon.GetNodeConfig(coordinates);

            TDNodeGenerator.Configure(node, tile, nodeConfig, dungeon);
        }

        private static Transform GetOrCreateElevationNode(TiledDungeon dungeon, int elevation)
        {
            var elevationNodeName = $"Elevation {elevation}";
            var parent = dungeon.LevelParent;
            Transform child;
            for (var i = 0; i < parent.childCount; i++)
            {
                child = parent.GetChild(i);
                if (child.name == elevationNodeName) return child;
            }

            child = new GameObject(elevationNodeName).transform;
            child.SetParent(parent);

            return child;
        }

        private static void GenerateAllNodeConfigsForLayer(TiledDungeon dungeon, int elevation)
        {
            var layerConfig = dungeon.GetLayerConfig(elevation);

            for (int row = 0; row < layerConfig.LayerSize.y; row++)
            {
                for (int col = 0; col < layerConfig.LayerSize.x; col++)
                {
                    var coordinates = layerConfig.AsUnityCoordinates(col, row);
                    var tile = layerConfig.GetTile(coordinates);

                    if (tile == null) continue;

                    /// Generate the node config
                    dungeon.GetNodeConfig(coordinates);
                }
            }

        }

        private static void GenerateLevel(TiledDungeon dungeon, int elevation)
        {
            var parent = GetOrCreateElevationNode(dungeon, elevation);

            var layerConfig = dungeon.GetLayerConfig(elevation);

            for (int row = 0; row < layerConfig.LayerSize.y; row++)
            {
                for (int col = 0; col < layerConfig.LayerSize.x; col++)
                {
                    GenerateNode(dungeon, layerConfig.AsUnityCoordinates(col, row), parent);
                }
            }

        }

        public static void GenerateMap(TiledDungeon dungeon)
        {
            dungeon.SyncNodes();

            dungeon.GetComponent<AbsInventory>()?.Configure(dungeon.MapName, null, -1);

            // We actually need the configs in place before we start or things will get quite
            // annoying in when spawning in stuff
            foreach (var elevation in dungeon.Elevations)
            {
                GenerateAllNodeConfigsForLayer(dungeon, elevation);
            }

            foreach (var elevation in dungeon.Elevations)
            {
                GenerateLevel(dungeon, elevation);
            }

            dungeon.SyncNodes();
        }
    }
}
