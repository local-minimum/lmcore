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
using LMCore.TiledDungeon.Style;



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
        float gridScale = 3f;
        public float GridSize => gridScale;

        [SerializeField]
        bool inferRoof = true;

        [SerializeField]
        public TDNode Prefab;

        [Header("Tiled")]
        [SerializeField] TiledMap map;

        public string MapName => map.Metadata.Name;

        [SerializeField] TiledTileset[] tilesets;

        [Header("Output")]
        [SerializeField, Tooltip("If empty, generated level will be placed under this node directly")]
        Transform _levelParent;
        public Transform LevelParent => _levelParent == null ? transform : _levelParent;


        [SerializeField]
        GridEntity Player;

        [SerializeField]
        TDNode SpawnTile;

        [SerializeField]
        Direction StartLookDirection;

        public AbsDungeonStyle Style;

        TDNode[] instancedNodes => LevelParent.GetComponentsInChildren<TDNode>();

        protected string PrefixLogMessage(string message) => $"TiledDungeon '{MapName}': {message}";

        Dictionary<Vector3Int, TDNode> _nodes;
        Dictionary<Vector3Int, TDNode> nodes
        {
            get
            {
                if (_nodes == null) SyncNodes();
                return _nodes;
            }
        }

        public void SyncNodes()
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

            set
            {
                nodes[coordinates] = value;
            }
        }

        IDungeonNode IDungeon.this[Vector3Int coordinates] { 
            get
            {
                return this[coordinates];
            }
        }

        public int NodeCount => nodes.Count;

        TiledNodeRoofRule Roofing(TDNode aboveNode, bool topLayer) {
            if (!inferRoof) return TiledNodeRoofRule.CustomProps;

            if (aboveNode == null)
            {
                return topLayer ? TiledNodeRoofRule.ForcedNotSet : TiledNodeRoofRule.ForcedSet;
            }

            return aboveNode.HasFloor || aboveNode.HasIllusion(Direction.Down) ? TiledNodeRoofRule.ForcedSet : TiledNodeRoofRule.ForcedNotSet;
        }

        Dictionary<int, TDLayerConfig> layerConfigs = new Dictionary<int, TDLayerConfig>();

        public TDLayerConfig GetLayerConfig(int elevation)
        {
            if (layerConfigs.ContainsKey(elevation)) return layerConfigs[elevation];

            var topLayer = Elevations.Max() == elevation;
            var config = new TDLayerConfig(map, tilesets, elevation, topLayer);
            layerConfigs[elevation] = config;

            return config;
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

        public IEnumerable<int> Elevations => map
            .FindInLayers(layer => layer.CustomProperties.Int(TiledConfiguration.instance.LayerElevationKey))
            .ToHashSet()
            .OrderByDescending(x => x);

        int IOnLoadSave.OnLoadPriority => 10000;

        [ContextMenu("Clean")]
        private void Clean() {
            LevelParent.DestroyAllChildren(DestroyImmediate);
#if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(LevelParent, "Clean level");
#endif
        }

        [ContextMenu("Regenerate")]
        private void Regenerate()
        {
            backupSettings = new GenerationBackupSettings(this);

            Clean();

            TDDungeonGenerator.GenerateMap(this);

            SpawnTile = this[backupSettings.SpawnCoordinates];

#if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(LevelParent, "Regenerate level");
#endif
        }

        [ContextMenu("Spawn")]
        private void Spawn()
        {
            if (Player.TransportationMode.HasFlag(TransportationMode.Flying) 
                || !SpawnTile.CanAnchorOn(Player, Direction.Down))
            {
                Player.Node = SpawnTile;
            } else
            {
                Player.NodeAnchor = SpawnTile.GetAnchor(Direction.Down);
            }
            Player.LookDirection = StartLookDirection;
            
            Player.Sync();
        }

        bool spawnPlayerAtLevelStart = true;
        private void Start()
        {
            if (spawnPlayerAtLevelStart)
            {
                Spawn();
            }
        }

        private void OnEnable()
        {
            Player.GridSizeProvider = this;
            Player.Dungeon = this;

            // TODO: replace with new systems
            /*
            var movementInterpreter = Player.EntityMovementInterpreter;
            movementInterpreter.Dungeon = this;

            foreach (var mover in Player.Movers)
            {
                mover.GridSizeProvider = this;
                mover.Dungeon = this;
            }*/

            Debug.Log(PrefixLogMessage("Enabled"));
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

        public Vector3 Position(GridEntity entity) =>
            HasNodeAt(entity.Coordinates) ?
                this[entity.Coordinates].GetEdge(entity.AnchorDirection) :
                entity.Coordinates.ToPosition(GridSize) + entity.AnchorDirection.AsLookVector3D().ToDirection(GridSize * 0.5f);

        public Vector3 Position(Vector3Int coordinates, Direction anchor, bool rotationRespectsAnchorDirection) =>
            HasNodeAt(coordinates) ? this[coordinates].GetEdge(anchor) :
            coordinates.ToPosition(GridSize) + anchor.AsLookVector3D().ToDirection(GridSize * 0.5f);

        void OnLoadGameSave(GameSave save)
        {
            if (save != null)
            {
                ItemDisposal.InstanceOrCreate().LoadFromSave(save.disposedItems);
                spawnPlayerAtLevelStart = false;
                Debug.Log(PrefixLogMessage("Save loaded"));
            }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }

        public GridEntity GetEntity(string identifier)
        {
            foreach (var entity in GetComponentsInChildren<GridEntity>())
            {
                if (entity.Identifier == identifier) return entity;
            }
            return null;
        }
    }
}
