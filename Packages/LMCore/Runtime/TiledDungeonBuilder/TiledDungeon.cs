using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledImporter;
using LMCore.TiledDungeon.Integration;
using System;
using LMCore.Inventory;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LMCore.TiledDungeon
{
    public class TiledDungeon : MonoBehaviour, IGridSizeProvider, IDungeon, IOnLoadSave
    {
        [Serializable]
        class GenerationBackupSettings
        {
            public Vector3Int SpawnCoordinates;

            public GenerationBackupSettings(TiledDungeon dungeon)
            {
                SpawnCoordinates = dungeon.SpawnTile?.Coordinates ?? dungeon?.backupSettings.SpawnCoordinates ?? Vector3Int.zero;
            }
        }

        [SerializeField, HideInInspector]
        GenerationBackupSettings backupSettings;

        [Header("Settings")]
        [SerializeField, Range(0, 10)]
        float scale = 3f;
        public float Scale => scale;

        [SerializeField]
        bool inferRoof = true;

        [SerializeField]
        TDNode Prefab;

        [Header("Tiled")]
        [SerializeField] TiledMap map;
        [SerializeField] TiledTileset[] tilesets;

        [Header("Output")]
        [SerializeField, Tooltip("If empty, generated level will be placed under this node directly")]
        Transform _levelParent;
        Transform levelParent => _levelParent == null ? transform : _levelParent;


        [SerializeField]
        GridEntity Player;

        [SerializeField]
        TDNode SpawnTile;

        [SerializeField]
        Direction StartLookDirection;

        public DungeonStyle Style;

        TDNode[] instancedNodes => levelParent.GetComponentsInChildren<TDNode>();

        public float GridSize => scale;

        Dictionary<Vector3Int, TDNode> _nodes;
        Dictionary<Vector3Int, TDNode> nodes
        {
            get
            {
                if (_nodes == null) SyncNodes();
                return _nodes;
            }
        }

        void SyncNodes()
        {
            if (_nodes == null)
            {
                _nodes = new Dictionary<Vector3Int, TDNode>();
            }
            var instanced = instancedNodes.ToHashSet();
            var recordedNodes = _nodes.Values.ToHashSet();

            foreach (var node in recordedNodes.Except(instanced)) {
                _nodes.Remove(node.Coordinates);
                if (node != null && node.gameObject != null)
                {
                    Destroy(node.gameObject);
                }
            }


            foreach (var node in instanced)
            {
                _nodes[node.Coordinates] = node;
            }
        }

        public void RemoveNode(TDNode node)
        {
            nodes.Remove(node.Coordinates);
        }

        public TDNode this[Vector3Int coordinates]
        {
            get
            {
                return nodes.GetValueOrDefault(coordinates);
            }
        }

        IDungeonNode IDungeon.this[Vector3Int coordinates] { 
            get
            {
                return this[coordinates];
            }
        }

        TDNode GetOrCreateNode(Vector3Int coordinates, Transform parent)
        {
            TDNode node;
            if (nodes.ContainsKey(coordinates))
            {
                node = nodes[coordinates];
                if (node.transform.parent != parent)
                {
                    node.transform.SetParent(parent);
                }
                return node;
            }

            node = Instantiate(Prefab, parent);
            node.Coordinates = coordinates;

            nodes.Add(coordinates, node);

            return node;
        }

        public int Size => nodes.Count;

        TiledNodeRoofRule Roofing(TDNode aboveNode, bool topLayer) {
            if (!inferRoof) return TiledNodeRoofRule.CustomProps;

            if (aboveNode == null)
            {
                return topLayer ? TiledNodeRoofRule.ForcedNotSet : TiledNodeRoofRule.ForcedSet;
            }

            return aboveNode.HasFloor ? TiledNodeRoofRule.ForcedSet : TiledNodeRoofRule.ForcedNotSet;
        }

        Dictionary<int, TDLayerConfig> layerConfigs = new Dictionary<int, TDLayerConfig>();

        TDLayerConfig GetLayerConfig(int elevation)
        {
            if (layerConfigs.ContainsKey(elevation)) return layerConfigs[elevation];

            var topLayer = elevations.Max() == elevation;
            var config = new TDLayerConfig(map, tilesets, elevation, topLayer);
            layerConfigs[elevation] = config;

            return config;
        }

        void GenerateNode(Vector3Int coordinates, Transform parent)
        {
            var layerConfig = GetLayerConfig(coordinates.y);
            var tile = layerConfig.GetTile(coordinates);

            if (tile == null) return;

            if (nodes.ContainsKey(coordinates)) return;

            var node = GetOrCreateNode(coordinates, parent);

            var nodeConfig = GetNodeConfig(coordinates);

            node.Configure(tile, nodeConfig, this);

        }

        Dictionary<Vector3Int, TDNodeConfig> nodeConfigurations = new Dictionary<Vector3Int, TDNodeConfig>();

        public TDNodeConfig GetNodeConfig(Vector3Int coordinates)
        {
            if (nodeConfigurations.ContainsKey(coordinates)) return nodeConfigurations[coordinates];

            var layerConfig = GetLayerConfig(coordinates.y);

            var aboveNode = this[coordinates + Vector3Int.up];
            var roofed = Roofing(aboveNode, layerConfig.TopLayer);
            var config = new TDNodeConfig(layerConfig, coordinates, roofed);
            nodeConfigurations[coordinates] = config;
            return config;
        }

        Transform GetOrCreateElevationNode(int elevation)
        {
            var elevationNodeName = $"Elevation {elevation}";
            var parent = levelParent;
            Transform child;
            for (var i = 0; i < parent.childCount; i++)
            {
                child = parent.GetChild(i);
                if (child.name == elevationNodeName) return child;
            }

            child = new GameObject(elevationNodeName).transform;
            child.SetParent(levelParent);

            return child;
        }

        void GenerateLevel(int elevation)
        {
            var parent = GetOrCreateElevationNode(elevation);

            var layerConfig = GetLayerConfig(elevation);

            for (int row = 0; row < layerConfig.LayerSize.y; row++)
            {
                for (int col = 0; col < layerConfig.LayerSize.x; col++)
                {
                    GenerateNode(layerConfig.AsUnityCoordinates(col, row), parent);
                }
            }
        }

        IEnumerable<int> elevations => map
            .FindInLayers(layer => layer.CustomProperties.Ints[TiledConfiguration.instance.LayerElevationKey])
            .ToHashSet()
            .OrderByDescending(x => x);

        int IOnLoadSave.OnLoadPriority => 10000;

        public void GenerateMap()
        {
            SyncNodes();

            GetComponent<AbsInventory>()?.Configure(map.Metadata.Name, null, -1);

            foreach (var elevation in elevations)
            {
                GenerateLevel(elevation);
            }

            SyncNodes();
        }

        [ContextMenu("Clean")]
        private void Clean() {
            levelParent.DestroyAllChildren(DestroyImmediate);
#if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(levelParent, "Clean level");
#endif
        }

        [ContextMenu("Regenerate")]
        private void Regenerate()
        {
            backupSettings = new GenerationBackupSettings(this);

            Clean();
            GenerateMap();

            SpawnTile = this[backupSettings.SpawnCoordinates];

#if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(levelParent, "Regenerate level");
#endif
        }

        [ContextMenu("Spawn")]
        private void Spawn()
        {
            Player.Position = SpawnTile.Coordinates;
            Player.LookDirection = StartLookDirection;
            Player.Anchor = Direction.Down;
            Player.Sync();
        }

        private void Start()
        {
            Spawn();
        }

        private void OnEnable()
        {
            Player.GridSizeProvider = this;
            Player.Dungeon = this;
            var movementInterpreter = Player.EntityMovementInterpreter;
            movementInterpreter.Dungeon = this;

            foreach (var mover in Player.Movers)
            {
                mover.GridSizeProvider = this;
                mover.Dungeon = this;
            }
        }

        public bool HasNodeAt(Vector3Int coordinates) => nodes.ContainsKey(coordinates);


        public List<IDungeonNode> FindTeleportersById(int id)
        {
            Func<TiledCustomProperties, bool> predicate = (props) => 
                props.Ints.GetValueOrDefault(TiledConfiguration.instance.TeleporterIdProperty) == id;

            return nodes.Values
                .Where(n => n.Config.HasObject(TiledConfiguration.instance.TeleporterClass , predicate))
                .Select(n => (IDungeonNode)n)
                .ToList();
        }

        public Vector3 DefaultAnchorOffset(Direction anchor, bool rotationRespectsAnchorDirection)
        {
            return TDNode.DefaultAnchorOffset(anchor, rotationRespectsAnchorDirection, GridSize);
        }

        void IOnLoadSave.OnLoad()
        {
            var save = TDSaveSystem<GameSave>.ActiveSaveData;

            ItemDisposal.InstanceOrCreate().LoadFromSave(save.disposedItems);
        }
    }
}
