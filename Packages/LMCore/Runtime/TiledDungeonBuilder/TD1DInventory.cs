using LMCore.Inventory;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    [System.Serializable]
    public class StackedItemInfo : ItemInfo
    {
        public int StackIndex;

        public StackedItemInfo(AbsItem item, int stackId) { 
            StackIndex = stackId;
            ItemId = item.Id;
            OriginId = item.Origin;
        }

        public StackedItemInfo(string id, string origin, int stackId) { 
            StackIndex = stackId;
            OriginId = origin;
            ItemId = id;
        }
    }

    public class TD1DInventory : SimpleInventory 
    {
        /// <summary>
        /// This load requires item disposal to have loaded first
        /// </summary>
        public int OnLoadPriority => 1000;

        public void OnLoad(InventorySave<StackedItemInfo> container)
        {
            var disposed = ItemDisposal.InstanceOrCreate().GetDisposed(FullId).ToList();
            var recylcer = RecycleBin.InstanceOrCreate();
            var dungeon = GetComponentInParent<TiledDungeon>();

            // 1. Dispose of already disposed items
            foreach (var item in stacks.SelectMany(stack => stack.Items).Where(item => disposed.Any(io => io.Equals(item))))
            {
                Remove(item);
                item.gameObject.SetActive(false);
                item.transform.SetParent(ItemDisposal.instance.transform);
            }

            if (container == null)
            {
                Debug.LogWarning(PrefixLogMessage("No save info exists for me"));
                return;
            }

            // 2. Recycle things no longer here for safety
            foreach (var item in stacks.SelectMany(stack => stack.Items).Where(item => !container.items.Any(info => info.Equals(item))).ToList()) {
                Remove(item);
                recylcer.Add(item);
                item.WorldRoot?.gameObject.SetActive(false);
            }

            // 3. Claim or create those items that weren't originally here
            foreach (var info in container.items.Where(info => info.OriginId != FullId))
            {
                if (recylcer.Remove(info.ItemId, info.OriginId, out AbsItem item))
                {
                    Add(item);
                } else
                {
                    var originalInventory = GetByFullId(info.OriginId);
                    if (originalInventory == null) {
                        if (SimpleItemFactory.InstanceOrCreate().Create(info.ItemId, info.OriginId, out item)) {
                            Add(item);
                        } else
                        {
                            Debug.LogError(PrefixLogMessage($"{info.ItemId} could not be recoverd from {info.OriginId} because inventory not known in scene and item not known by factory"));
                        }
                    } else
                    {
                        if (originalInventory.Remove(info.ItemId, out item))
                        {
                            Add(item);
                        } else
                        {
                            Debug.LogError(PrefixLogMessage($"{info.ItemId} should have existed in {originalInventory.FullId} but it refused to give us one, so will try to make new one"));
                            if (SimpleItemFactory.InstanceOrCreate().Create(info.ItemId, info.OriginId, out item)) {
                                Add(item);
                            } else
                            {
                                Debug.LogError(PrefixLogMessage($"{info.ItemId} could not be created by factory"));
                            }
                        }
                    }
                }
            }

            // 4. Order inventory according to save
            int stackIdx = 0;
            foreach (var stack in stacks)
            {
                foreach (var item in stack.Items.ToList()) { 
                    var info = container.items.FirstOrDefault(info => info.Equals(item));
                    if (info == null)
                    {
                        Debug.LogError(PrefixLogMessage($"No save info found for {item.Id}, it should not be possible"));
                        continue;
                    }

                    if (info.StackIndex != stackIdx)
                    {
                        stack.Remove(item);

                        AddToStack(info.StackIndex, item);
                    }
                }

                stackIdx++;
            }
        }

        public IEnumerable<StackedItemInfo> Save() =>
            stacks.SelectMany((stack, idx) => stack.Items.Select(item => new StackedItemInfo(item, idx)));
    }
}
