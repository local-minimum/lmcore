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
        GridEntity _Player;
        public GridEntity Player => _Player;

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
                if (value == null)
                {
                    Debug.LogWarning($"Setting a node to null at {coordinates}");
                }
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

            // TODO: Something is iffy here with asking for configs that will not be complete
            var aboveNodeCoordinates = coordinates + Vector3Int.up;
            bool storeConfig = true;
            if (!layerConfig.TopLayer && !HasNodeAt(aboveNodeCoordinates))
            {
                Debug.LogWarning($"Asking for a node outside the dungeon at {aboveNodeCoordinates}");
                storeConfig = false;
            }
            var aboveNode = this[aboveNodeCoordinates];
            var roofed = Roofing(aboveNode, layerConfig.TopLayer);
            var config = new TDNodeConfig(layerConfig, coordinates, roofed);
            if (storeConfig) nodeConfigurations[coordinates] = config;
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
            if (_Player.TransportationMode.HasFlag(TransportationMode.Flying) 
                || !SpawnTile.CanAnchorOn(_Player, Direction.Down))
            {
                _Player.Node = SpawnTile;
            } else
            {
                _Player.NodeAnchor = SpawnTile.GetAnchor(Direction.Down);
            }
            _Player.LookDirection = StartLookDirection;
            
            _Player.Sync();
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
            _Player.GridSizeProvider = this;
            _Player.Dungeon = this;

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

        [System.Serializable]
        public struct Checkpoint
        {
            public Vector3Int Coordinates;
            public Direction Anchor;

            public bool IsHere(GridEntity entity) =>
                entity.Coordinates == Coordinates && entity.AnchorDirection == Anchor;

            public override string ToString() =>
                $"<{Coordinates} {Anchor}>";
        }

        [System.Serializable]
        public struct Translation 
        {
            public Direction TranslationHere;
            public Checkpoint Checkpoint;

            public override string ToString() =>
                $"[{TranslationHere} -> {Checkpoint}]";
        }

        /// <summary>
        /// Closest path for the entity to get to target. Not regarding cost of turns.
        /// </summary>
        /// <param name="entity">Who wants to travel</param>
        /// <param name="start">Start of the search</param>
        /// <param name="target">Search target</param>
        /// <param name="maxDepth">Abort search if not found with these may steps</param>
        /// <param name="path">The path to the target excluding the start coordinates</param>
        /// <returns></returns>
        public bool ClosestPath(
            GridEntity entity,
            Vector3Int start,
            Vector3Int target,
            int maxDepth,
            out List<Translation> path,
            bool debugLog = false)
        {
            // TODO: We don't account for just waiting on a moving platform to reach a goal either
            // nor do we have a way to attatch goals to faces that can move...

            var seen = new Dictionary<Checkpoint, List<Translation>>();
            var seenQueue = new Queue<Checkpoint>();
            var visited = new HashSet<Checkpoint>();

            var startCheckpoint = new Checkpoint() { Anchor = entity.AnchorDirection, Coordinates = start };
            seenQueue.Enqueue(startCheckpoint);

            while (seenQueue.Count > 0)
            {
                var checkpoint = seenQueue.Dequeue();
                if (visited.Contains(checkpoint))
                {
                    if (debugLog) Debug.Log($"ClosestPath: Ignore {checkpoint} because already visited");
                    continue;
                }

                visited.Add(checkpoint);

                var pathHere = seen.GetValueOrDefault(checkpoint) ?? new List<Translation>();
                
                // Stop searching if we are too deep in
                if (pathHere.Count > maxDepth)
                {
                    if (debugLog) Debug.Log($"ClosestPath: Aborting because exceeding maxDepth({maxDepth})");
                    path = null;
                    return false;
                }

                var node = this[checkpoint.Coordinates];
                if (node == null)
                {
                    if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint} because there's no node there");
                    path = null;
                    continue;
                }

                foreach (var direction in DirectionExtensions.AllDirections)
                {
                    // If we are in the air but no flying capability we should only consider down
                    if (entity.TransportationMode.HasFlag(TransportationMode.Walking) && 
                        (checkpoint.Anchor == Direction.None || checkpoint.Anchor == Direction.Down) && 
                        !node.HasFloor)
                    {
                        if (direction != Direction.Down)
                        {
                            if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint}-{direction} because not flying and no floor");
                            continue;
                        }
                    }

                    if (direction.IsParallell(checkpoint.Anchor) && !entity.TransportationMode.HasFlag(TransportationMode.Flying))
                    {
                        if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint}-{direction} because not flying");
                        continue;
                    }

                    var outcome = node.AllowsTransition(entity, checkpoint.Coordinates, checkpoint.Anchor, direction, out var neighbourCoordinates, out var neighbourAnchor, false);
                    if (outcome == MovementOutcome.Refused || outcome == MovementOutcome.Blocked)
                    {
                        if (debugLog) Debug.Log($"ClosestPath: Direction {checkpoint}-{direction} got {outcome}");
                        continue;
                    }

                    var anchor = node.GetAnchor(checkpoint.Anchor);
                    if (outcome == MovementOutcome.NodeExit && neighbourAnchor != null && anchor != null)
                    {
                        // Exclude transitions that would require climbing more than entity can do
                        var nodeEdge = anchor.GetEdgePosition(direction);
                        var neigbourEdge = neighbourAnchor.GetEdgePosition(direction.Inverse());
                        var up = entity.Down.Inverse().AsLookVector3D();
                        var delta = neigbourEdge - nodeEdge;
                        if (Vector3.Dot(delta, up) > entity.Abilities.maxScaleHeight)
                        {
                            if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint}-{direction} because too high step up {Vector3.Dot(delta, up)} > {entity.Abilities.maxScaleHeight}");
                            continue;
                        }

                        // TODO: Exclude jumps that are too long for the entity
                    }

                    var neighbour = new Checkpoint() { Anchor = neighbourAnchor?.CubeFace ?? Direction.None, Coordinates = neighbourCoordinates };
                    // TODO: How to handle if there's no anchor on the neighbour?

                    if (seen.ContainsKey(neighbour))
                    {
                        if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint}-{direction} because we have a closer path to {neighbour}");
                        continue;
                    }

                    var neighbourPath = new List<Translation>(pathHere);
                    neighbourPath.Add(new Translation() { TranslationHere = direction, Checkpoint = neighbour });

                    if (neighbour.Coordinates == target)
                    {
                        path = neighbourPath;
                        return true;
                    }
                    
                    seen[neighbour] = neighbourPath; 
                    seenQueue.Enqueue(neighbour);
                }
            }

            path = null;
            return false;
        }
    }
}
