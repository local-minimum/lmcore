using System.Collections.Generic;
using UnityEngine;
using TiledImporter;
using System.Linq;
using LMCore.Crawler;
using LMCore.Extensions;
using System;

namespace TiledDungeon
{
    public partial class TiledDungeon : MonoBehaviour, IGridSizeProvider, IDungeon
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

        [SerializeField]
        Direction StartLookDirection;

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

        TDNode this[Vector3Int coordinates]
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

        TDNode GetOrCreateNode(Vector3Int coordinates)
        {
            if (nodes.ContainsKey(coordinates)) return nodes[coordinates];

            var node = Instantiate(Prefab, levelParent);
            node.Coordinates = coordinates;

            nodes.Add(coordinates, node);

            return node;
        }

        public int Size => nodes.Count;

        public Vector3Int AsUnityCoordinates(Vector2Int layerSize, int col, int row, int elevation) =>
            new Vector3Int(col, elevation, layerSize.y - row - 1);

        TiledNodeRoofRule Roofing(TDNode aboveNode, bool topLayer) {
            if (!inferRoof) return TiledNodeRoofRule.CustomProps;

            if (aboveNode == null)
            {
                return topLayer ? TiledNodeRoofRule.ForcedNotSet : TiledNodeRoofRule.ForcedSet;
            }

            return aboveNode.HasFloor ? TiledNodeRoofRule.ForcedSet : TiledNodeRoofRule.ForcedNotSet;
        }

        void GenerateLevel(TiledLayer layer, int elevation, List<TiledLayer> modifiers, List<TiledObjectLayer> objectLayers, bool topLayer)
        {
            var layerSize = layer.LayerSize;
            Func<Vector2Int, Vector2Int> inverseCoordinates = (Vector2Int v) => { return new Vector2Int(v.x, layerSize.y - v.y - 1); };

            for (int row = 0; row < layerSize.y; row++)
            {
                for (int col = 0; col < layerSize.x; col++)
                {
                    var tileId = layer[row, col];
                    var tile = map.GetTile(tileId, tilesets);

                    if (tile == null) continue;

                    var coordinates = AsUnityCoordinates(layerSize, col, row, elevation);

                    var node = GetOrCreateNode(coordinates);
                    var aboveNode = this[node.Coordinates + Vector3Int.up];
                    var roofed = Roofing(aboveNode, topLayer);

                    var modifications = modifiers
                        .Select(modLayer =>
                        {
                            if (!modLayer.InsideLayer(row, col)) return null;

                            var modId = modLayer[row, col];
                            var tile = map.GetTile(modId, tilesets);

                            if (tile == null) return null;

                            Debug.Log($"Modification for {node.Coordinates} {tile.Type}");
                            return new TileModification() { 
                                Layer = modLayer.Name, 
                                LayerProperties = modLayer.CustomProperties, 
                                Tile = tile 
                            };
                        })
                        .Where(tm => tm != null)
                        .ToArray();

                    var tileRect = inverseCoordinates(coordinates.To2DInXZPlane())
                        .ToUnitRect();

                    var points = objectLayers
                        .SelectMany(l => l.Points)
                        .Where(p => p.Applies(tileRect))
                        .ToArray();

                    var rects = objectLayers
                        .SelectMany(l => l.Rects)
                        .Where(r => r.Applies(tileRect))
                        .ToArray();

                    node.Configure(
                        tile,
                        roofed,
                        this,
                        modifications,
                        points,
                        rects
                    );
                }
            }
        }

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
                var objectLayers = map
                    .FindObjectLayers(layer => layer.CustomProperties.Ints[elevationProperty] == elevation)
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

                GenerateLevel(layoutLayer, elevation, modificationLayers, objectLayers, topLayer);

                topLayer = false;
            }

            SyncNodes();
        }

        [ContextMenu("Clean")]
        private void Clean() {
            transform.DestroyAllChildren(DestroyImmediate);
        }

        [ContextMenu("Regenerate")]
        private void Regenerate()
        {
            var spawnCoordinates = SpawnTile.Coordinates;
            Clean();
            GenerateMap();
            SpawnTile = this[spawnCoordinates];
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
    }
}
