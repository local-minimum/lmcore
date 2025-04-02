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
using LMCore.TiledDungeon.Enemies;
using UnityEngine.SceneManagement;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LMCore.TiledDungeon
{
    public delegate void DungeonLoadEvent(TiledDungeon dungeon, bool fromSave);
    public delegate void DungeonUnloadEvent(TiledDungeon dungeon);

    public partial class TiledDungeon : MonoBehaviour, IGridSizeProvider, IDungeon, IOnLoadSave
    {
        public static event DungeonLoadEvent OnDungeonLoad;
        public static event DungeonUnloadEvent OnDungeonUnload;

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
        public TiledMap Map => map;

        public string MapName
        {
            get
            {
                if (map == null)
                {
                    Debug.LogError($"{name} dungeon doesn't have a map assigned");
                    return null;
                }
                else if (map.Metadata == null)
                {
                    Debug.LogError($"{name} metadata is nothing");
                    return null;
                }
                return map.Metadata.Name;
            }
        }

        [SerializeField] TiledTileset[] tilesets;

        [Header("Output")]
        [SerializeField, Tooltip("If empty, generated level will be placed under the dungeon node directly")]
        Transform _levelParent;
        public Transform LevelParent => _levelParent == null ? transform : _levelParent;


        [SerializeField, Tooltip("If empty, decoration under the dungeon node directly while generating the dungeon. Important that not same as level parent!")]
        Transform _decorationStorage;
        public Transform DecorationStorage => _decorationStorage == null ? transform : _decorationStorage;

        [SerializeField]
        GridEntity _Player;
        public GridEntity Player => _Player;
        public IEnumerable<GridEntity> Enemies => GetComponentsInChildren<GridEntity>()
            .Where(entity => entity.EntityType == GridEntityType.Enemy)
            .Select(entity => new { entity, enemy = entity.GetComponent<TDEnemy>() })
            .Where(item => item.enemy != null && item.enemy.Stats.IsAlive)
            .Select(item => item.entity);

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

        LevelFloorContainer _floorContainer;
        public LevelFloorContainer floorContariner
        {
            get
            {
                if (_floorContainer == null)
                {
                    _floorContainer = GetComponent<LevelFloorContainer>();
                }
                return _floorContainer;
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

            foreach (var node in recordedNodes.Except(instanced))
            {
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

        IDungeonNode IDungeon.this[Vector3Int coordinates]
        {
            get
            {
                return this[coordinates];
            }
        }

        public int NodeCount => nodes.Count;

        TiledNodeRoofRule Roofing(TDNode aboveNode, bool topLayer)
        {
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

        public TDNodeConfig GetNodeConfig(Vector3Int coordinates, bool warnMissingAboveNode = true)
        {
            if (nodeConfigurations.ContainsKey(coordinates)) return nodeConfigurations[coordinates];

            var layerConfig = GetLayerConfig(coordinates.y);

            // TODO: Something is iffy here with asking for configs that will not be complete
            var aboveNodeCoordinates = coordinates + Vector3Int.up;
            bool storeConfig = true;
            if (!layerConfig.TopLayer && !HasNodeAt(aboveNodeCoordinates))
            {
                if (warnMissingAboveNode) Debug.LogWarning($"Asking for a node outside the dungeon at {aboveNodeCoordinates}");
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
        private void Clean()
        {
            if (Player != null)
            {
                Player.transform.SetParent(transform);
            }

            if (LevelParent == transform)
            {
                Debug.LogError(PrefixLogMessage("Cannot clean a level generated directly under the dungeon node!"));
                return;
            }

            var decorationStorage = DecorationStorage;
            foreach (var decoration in LevelParent.GetComponentsInChildren<TDDecoration>(true))
            {
                decoration.Remove(decorationStorage);
            }

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

            foreach (var decoration in DecorationStorage.GetComponentsInChildren<TDDecoration>(true))
            {
                decoration.Place(this);
            }

            SpawnTile = this[backupSettings.SpawnCoordinates];

#if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(LevelParent, "Regenerate level");
#endif
        }

        public void Live(TiledMap map)
        {
            Regenerate();
            Debug.Log(PrefixLogMessage($"Live Edited map"));
        }

        [ContextMenu("Spawn")]
        private void Spawn()
        {
            if (_Player.TransportationMode.HasFlag(TransportationMode.Flying)
                || !SpawnTile.CanAnchorOn(_Player, Direction.Down))
            {
                _Player.Node = SpawnTile;
            }
            else
            {
                _Player.NodeAnchor = SpawnTile.GetAnchor(Direction.Down);
            }
            _Player.LookDirection = StartLookDirection;

            _Player.Sync();
        }

        bool loadedSavedGame = false;

        private void Start()
        {
            if (!loadedSavedGame)
            {
                Spawn();
            }
        }

        private void OnEnable()
        {
            StartCoroutine(EmitDungeonLoaded());
        }

        IEnumerator<WaitForSeconds> EmitDungeonLoaded()
        {
            yield return new WaitForSeconds(0.02f);

            OnDungeonLoad?.Invoke(this, loadedSavedGame);
            Debug.Log(PrefixLogMessage("Dungeon loaded"));
        }

        private void OnDisable()
        {
            OnDungeonUnload?.Invoke(this);
        }

        private void OnDestroy()
        {
            OnDungeonUnload?.Invoke(this);
        }

        public bool HasNodeAt(Vector3Int coordinates) => nodes.ContainsKey(coordinates);


        public List<IDungeonNode> FindTeleportersById(int id)
        {
            Func<TiledCustomProperties, bool> predicate = (props) =>
                props.Ints.GetValueOrDefault(TiledConfiguration.instance.TeleporterIdProperty) == id;

            return nodes.Values
                .Where(n => n.Config.HasObject(TiledConfiguration.instance.TeleporterClass, predicate))
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
                loadedSavedGame = true;
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

        public void RemoveGridEntityFromDungeon(GridEntity entity)
        {
            foreach (var node in nodes.Values)
            {
                if (node.Occupants.Contains(entity)) node.RemoveOccupant(entity);
                node.RemoveReservation(entity);
            }

            entity.enabled = false;
        }

        public GridEntity RemoveGridEntityFromDungeon(System.Func<GridEntity, bool> predicate)
        {
            var entity = GetComponentsInChildren<GridEntity>().First(e => predicate(e));
            if (entity != null)
            {
                RemoveGridEntityFromDungeon(entity);
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage("Could not find any enity matching predicate"));
            }

            return entity;
        }

        /// <summary>
        /// Closest path for the entity to get to target starting with the current entity position
        /// and ending with the target position.
        /// 
        /// Not regarding cost of turns.
        /// </summary>
        /// <param name="entity">Who wants to travel</param>
        /// <param name="start">Start of the search</param>
        /// <param name="target">Search target</param>
        /// <param name="maxDepth">Abort search if not found with these may steps</param>
        /// <param name="refuseSafeZones">If search is allowed to go through safe zones</param>
        /// <param name="path">The path to the target excluding the start coordinates</param>
        /// <returns>If path is found</returns>
        public bool ClosestPath(
            GridEntity entity,
            Vector3Int start,
            Vector3Int target,
            int maxDepth,
            out List<PathTranslation> path,
            bool refuseSafeZones = false,
            bool debugLog = false)
        {
            // TODO: We don't account for just waiting on a moving platform to reach a goal either
            // nor do we have a way to attatch goals to faces that can move...

            var seen = new Dictionary<PathCheckpoint, List<PathTranslation>>();
            var seenQueue = new Queue<PathCheckpoint>();
            var visited = new HashSet<PathCheckpoint>();

            var startCheckpoint = new PathCheckpoint() { Anchor = entity.AnchorDirection, Coordinates = start };
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

                var pathHere = seen.GetValueOrDefault(checkpoint) ?? new List<PathTranslation>();

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
                    continue;
                }

                foreach (var direction in DirectionExtensions.AllDirections)
                {
                    // If we are not falling or flying, lets check abilities if the anchor is allowed to us
                    var anchor = node.GetAnchor(checkpoint.Anchor);
                    if (checkpoint.Anchor != Direction.None && (
                        anchor == null ||
                        !anchor.Traversal.CanBeTraversedBy(entity)))
                    {
                        if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint}-{direction} because not flying and no valid anchor");
                        continue;
                    }
                    else if (checkpoint.Anchor == Direction.Down && !node.HasFloor)
                    {
                        if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint}-{direction} because no floor");
                        continue;
                    }

                    /*
                    if (direction.IsParallell(checkpoint.Anchor) && !entity.TransportationMode.HasFlag(TransportationMode.Flying))
                    {
                        if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint}-{direction} because not flying");
                        continue;
                    }
                    */

                    var outcome = node.AllowsTransition(entity, checkpoint.Coordinates, checkpoint.Anchor, direction, out var neighbourCoordinates, out var neighbourAnchor, false);
                    if (outcome == MovementOutcome.Refused || outcome == MovementOutcome.Blocked || (refuseSafeZones && TDSafeZone.In(neighbourCoordinates)))
                    {
                        if (debugLog) Debug.Log($"ClosestPath: Direction {checkpoint}-{direction} got {outcome}, SafeZone({TDSafeZone.In(neighbourCoordinates)})");
                        continue;
                    }

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

                    var neighbour = new PathCheckpoint() { Anchor = neighbourAnchor?.CubeFace ?? Direction.None, Coordinates = neighbourCoordinates };
                    // TODO: How to handle if there's no anchor on the neighbour?

                    if (seen.ContainsKey(neighbour))
                    {
                        if (debugLog) Debug.Log($"ClosestPath: Ignoring {checkpoint}-{direction} because we have a closer path to {neighbour}");
                        continue;
                    }

                    var neighbourPath = new List<PathTranslation>(pathHere);
                    neighbourPath.Add(new PathTranslation() { TranslationHere = direction, Checkpoint = neighbour });

                    if (neighbour.Coordinates == target)
                    {
                        path = new List<PathTranslation>() { new PathTranslation() { Checkpoint = startCheckpoint, TranslationHere = Direction.None } };
                        path.AddRange(neighbourPath);
                        return true;
                    }

                    seen[neighbour] = neighbourPath;
                    seenQueue.Enqueue(neighbour);
                }
            }

            path = null;
            return false;
        }

        [ContextMenu("Info")]
        void Info()
        {
            var layers = map
                .IterateAllLayers
                .Select(l => new { layer = l, elevation = l.CustomProperties.Int(TiledConfiguration.instance.LayerElevationKey) })
                .GroupBy(l => l.elevation)
                .Select(g =>
                {
                    var first = g.First();
                    return new { elevation = first.elevation, parts = string.Join(", ", g.Select(l => $"'{l.layer.Name}'")) };
                })
                .OrderByDescending(e => e.elevation)
                .Select(e => $"Elevation {e.elevation}: {e.parts}");

            Debug.Log(PrefixLogMessage($"Layers:\n{string.Join("\n", layers)}"));
        }

        [ContextMenu("Info: Entity occupancies")]
        void InfoOccupancies()
        {
            var nodes = this.nodes;

            var entities = GetComponentsInChildren<GridEntity>();
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var node = nodes.GetValueOrDefault(entity.Coordinates);
                if (node != (entity.Node as TDNode))
                {
                    Debug.LogWarning($"{entity.name}: Its node {entity.Node} isn't the same as indicated by its coordinates");
                }
                else if (node.Occupants.Contains(entity))
                {
                    Debug.Log($"{entity.name}: Is occupying {node.Coordinates}");
                }
                else
                {
                    Debug.LogWarning($"{entity.name}: Is at {node.Coordinates} but doesn't occupy it!");
                }

                foreach (var otherNode in nodes.Values)
                {
                    if (otherNode == node)
                    {
                        if (otherNode.Reservations.Contains(entity))
                        {
                            Debug.LogWarning($"{entity.name}: Is at {node.Coordinates} but only with a reservation");
                        }

                        continue;
                    }

                    if (otherNode.Reservations.Contains(entity))
                    {
                        Debug.Log($"{entity.name}: Is at {node.Coordinates} and has a reservation for {otherNode.Coordinates}");
                    }

                    if (otherNode.Occupants.Contains(entity))
                    {
                        Debug.LogError($"{entity.name}: Is at {node.Coordinates} but {otherNode.Coordinates} still have them as occupants");
                    }
                }
            }
        }

        [DevConsole.Command(new string[] { "dungeon" }, "Dungeon stuff")]
        private static void DCDungeonContext(string payload)
        {
            DevConsole.DevConsole.AddContext("dungeon");

            if (!string.IsNullOrEmpty(payload?.Trim()))
            {
                DevConsole.DevConsole.ProcessInput(payload);
                DevConsole.DevConsole.RemoveOuterContext("dungeon");
            }
            else
            {
                DevConsole.DevConsole.ListCommands();
            }
        }

        [DevConsole.Command(new string[] { "dungeon", "reload" }, "Restart current dungeon")]
        private static void DCReload(string payload)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
