using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public class LevelFloorContainer : MonoBehaviour
    {
        IDungeon _dungeon;
        IDungeon Dungeon
        {
            get
            {
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<IDungeon>();
                }
                return _dungeon;
            }
        }

        public IEnumerable<FloorLootNode> AllNodes =>
            GetComponentsInChildren<FloorLootNode>();

        public IEnumerable<FloorLootNode> NodesWithStuff =>
            AllNodes.Where(n => !n.inventory.Empty);

        public FloorLootNode GetNode(Vector3Int coordinates)
        {
            var dungeon = Dungeon;
            if (dungeon.HasNodeAt(coordinates))
            {
                var anchor = dungeon[coordinates].GetAnchor(Direction.Down);
                if (anchor == null) return null;

                return anchor.GetComponentInChildren<FloorLootNode>();
            }
            return null;
        }

        private void OnEnable()
        {
            foreach (var node in AllNodes)
            {
                node.Sync();
            }
        }
    }
}
