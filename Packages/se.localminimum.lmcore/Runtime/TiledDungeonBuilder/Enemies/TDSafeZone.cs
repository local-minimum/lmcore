using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDSafeZone : MonoBehaviour
    {
        private static HashSet<TDSafeZone> _Zones;
        private static HashSet<TDSafeZone> Zones
        {
            get
            {
                if (_Zones == null)
                {
                    _Zones = GameObject.FindObjectsByType<TDSafeZone>(FindObjectsSortMode.None).ToHashSet();
                }

                return _Zones;
            }
        }

        public static bool In(GridEntity entity) => In(entity.Coordinates);
        public static bool In(Vector3Int coordinates) => Zones.Any(z => z.ZoneNodes.Contains(coordinates));

        TDNode _Node;
        TDNode Node
        {
            get
            {
                if (_Node == null)
                {
                    _Node = GetComponentInParent<TDNode>();
                }
                return _Node;
            }
        }

        IEnumerable<Vector3Int> FFill() =>
            FloodFill.Fill(Node.Dungeon, Node.Coordinates, Size - 1);


        [SerializeField, Range(1, 10)]
        int Size = 2;

        public void Configure(int size)
        {
            Size = size;
            _ZoneNodes = FFill().ToHashSet();
        }

        HashSet<Vector3Int> _ZoneNodes;
        HashSet<Vector3Int> ZoneNodes
        {
            get
            {
                if (_ZoneNodes == null)
                {
                    _ZoneNodes = FFill().ToHashSet();
                }
                return _ZoneNodes;
            }
        }

        [ContextMenu("Recalculate")]
        private void Info()
        {
            _ZoneNodes = null;
            Debug.Log($"Safe Zone {Node.Coordinates}: {string.Join(", ", ZoneNodes)}");
        }

        private void OnEnable()
        {
            Zones.Add(this);
        }

        private void OnDisable()
        {
            Zones.Remove(this);
        }

        private void OnDestroy()
        {
            Zones.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (Node == null) return;

            var dungeon = Node.Dungeon;

            var size = dungeon.GridSize;
            var size3D = new Vector3(size, size, size);

            Gizmos.color = Color.green;

            foreach (var coordinates in ZoneNodes)
            {
                var node = dungeon[coordinates];
                var center = node.CenterPosition;
                Gizmos.DrawWireCube(center, size3D);
            }
        }
    }
}
