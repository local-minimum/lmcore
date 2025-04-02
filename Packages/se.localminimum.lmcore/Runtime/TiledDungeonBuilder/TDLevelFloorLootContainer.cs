using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDLevelFloorLootContainer : LevelFloorContainer, IOnLoadSave
    {
        #region Save/Load
        // Must be lower than moving platforms
        public int OnLoadPriority => 9;

        const int illegalValue = -99999;
        public void OnLoad(GameSave save)
        {
            if (save == null) return;

            var lvl = GetComponentInParent<IDungeon>().MapName;
            var lvlSave = save.levels.GetValueOrDefault(lvl);
            if (lvlSave == null)
            {
                return;
            }

            var floorSave = lvlSave.floorLoot;

            if (floorSave == null)
            {
                return;
            }

            foreach (var inventorySave in floorSave.inventories)
            {
                if (inventorySave.items == null || inventorySave.items.Count == 0 || inventorySave.fullId == null) continue;
                var id = inventorySave.fullId;
                var coordParts = id
                    .Split(":")
                    .Select(part => int.TryParse(part, out int value) ? value : illegalValue)
                    .ToList();

                if (coordParts.Count == 3 && coordParts.All(v => v != illegalValue))
                {
                    var coordinates = new Vector3Int(coordParts[0], coordParts[1], coordParts[2]);
                    var node = GetNode(coordinates);
                    if (node != null)
                    {
                        var inventory = node.inventory as TD1DInventory;
                        if (inventory != null)
                        {
                            inventory.OnLoad(inventorySave);
                        }
                        else
                        {
                            Debug.LogWarning($"FloorLoot: Could not load save for {coordinates} because inventory type not handled: {inventory}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"FloorLoot: Could not load save for {coordinates} because there's no node there");
                    }
                }
                else
                {
                    Debug.LogWarning($"FloorLoot: Could not load save for '{id}' because it's unexpeced format");
                }
            }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save != null)
            {
                OnLoad(save as GameSave);
            }
        }
        #endregion
    }
}
