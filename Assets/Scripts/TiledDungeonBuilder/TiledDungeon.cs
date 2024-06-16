using System.Collections.Generic;
using UnityEngine;
using TiledImporter;
using System.Linq;
using LMCore.Crawler;
using LMCore.Extensions;

namespace TiledDungeon
{
    public class TiledDungeon : MonoBehaviour, IGridSizeProvider
    {
        [Header("Settings")]
        [SerializeField, Range(0, 10)]
        float scale = 3f;
        public float Scale => scale;

        [SerializeField, Tooltip("Elevation Property, needs to be a custom int property")]
        string elevationProperty = "Elevation";

        [SerializeField]
        bool inferRoof = true;

        [SerializeField, Tooltip("Layers that start with this will be identified as primary layer for dungeon layout")]
        string layoutLayerPrefix = "dungeon";

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

        TDNode[] instancedNodes => levelParent.GetComponentsInChildren<TDNode>();

        public float GridSize => scale;

        Dictionary<Vector3Int, TDNode> _nodes;

        void SyncNodes()
        {
            if (_nodes == null)
                _nodes = new Dictionary<Vector3Int, TDNode>();

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
            if (_nodes == null) SyncNodes();

            _nodes.Remove(node.Coordinates);
        }

        TDNode this[Vector3Int coordinates]
        {
            get
            {
                if (_nodes == null) SyncNodes();

                return _nodes.GetValueOrDefault(coordinates);
            }
        }
        TDNode GetOrCreateNode(Vector3Int coordinates)
        {
            if (_nodes == null) SyncNodes();

            if (_nodes.ContainsKey(coordinates)) return _nodes[coordinates];

            var node = Instantiate(Prefab, levelParent);

            _nodes.Add(coordinates, node);

            return node;
        }

        // TODO: Support modifiers
        void GenerateLevel(TiledLayer layer, int elevation, List<TiledLayer> modifiers, bool topLayer)
        {
            var size = layer.LayerSize;
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var tileId = layer[y, x];
                    var tile = map.GetTile(tileId, tilesets);

                    if (tile == null) continue;

                    var coordinates = new Vector3Int(x, elevation, size.y - y - 1);
                    var node = GetOrCreateNode(coordinates);
                    Debug.Log($"{layer.Name} {elevation} {topLayer}");
                    var aboveNode = this[node.Coordinates + Vector3Int.up];
                    var roofed = inferRoof ? 
                        ((aboveNode?.HasFloor ?? !topLayer) ? TiledNodeRoofRule.ForcedSet : TiledNodeRoofRule.ForcedNotSet) 
                        : TiledNodeRoofRule.CustomProps;

                    node.Configure(
                        coordinates,
                        tile,
                        roofed,
                        this
                    );
                }
            }
        }

        [ContextMenu("Generate")]
        public void GenerateMap()
        {
            SyncNodes();

            var elevations = map
                .FindInLayers(layer => layer.CustomProperties.Ints[elevationProperty])
                .ToHashSet()
                .OrderByDescending(x => x)
                .ToArray();

            bool topLayer = true;

            foreach (var elevation in elevations)
            {
                var layers = map
                    .FindLayers(layer => layer.CustomProperties.Ints[elevationProperty] == elevation)
                    .ToList();
                var layoutLayer = layers.FirstOrDefault(l => l.Name.StartsWith(layoutLayerPrefix));

                if (layoutLayer == null)
                {
                    Debug.LogError(
                        $"No layout layer found for elevation {elevation} ({string.Join(", ", layers.Select(l => l.Name))})"
                    );
                    continue;
                }

                var modificationLayers = layers.Where(l => l != layoutLayer).ToList();

                GenerateLevel(layoutLayer, elevation, modificationLayers, topLayer);

                topLayer = false;
            }

            SyncNodes();
        }

        [ContextMenu("Clean")]
        private void Clean() {
            transform.DestroyAllChildren(DestroyImmediate);
        }

        [ContextMenu("Spawn")]
        private void Spawn()
        {
            Player.Position = SpawnTile.Coordinates;
            Player.Sync();
        }

        private void Start()
        {
            Spawn();
        }

        private void OnEnable()
        {
           foreach (var mover in Player.Movers)
            {
                mover.OnMoveEnd += Mover_OnMoveEnd;
                mover.GridSizeProvider = this;
            }
        }

        private void OnDisable()
        {
           foreach (var mover in Player.Movers)
            {
                mover.OnMoveEnd += Mover_OnMoveEnd;
            }
        }

        private void Mover_OnMoveEnd(GridEntity entity, LMCore.IO.Movement movement, Vector3Int startPosition, Direction startDirection, Vector3Int endPosition, Direction endDirection, bool allowed)
        {
            var node = this[endPosition];
            if (node == null) {
                Debug.LogError($"Player is at {endPosition}, which is outside the map");
                return;
            }

            if (!entity.transportationMode.HasFlag(TransportationMode.Flying) && !node.HasFloor)
            {
                entity.Falling = true;
            } else if (entity.Falling)
            {
                entity.Falling = false;
            }
        }

    }
}
