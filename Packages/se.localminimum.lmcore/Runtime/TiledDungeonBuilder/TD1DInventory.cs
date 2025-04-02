using LMCore.Inventory;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TD1DInventory : SimpleInventory
    {
        [SerializeField]
        List<EquipmentType> predefinedEquipmentStacks = new List<EquipmentType>();

        [SerializeField]
        bool predefinedAreSingleItem;

        private void Awake()
        {
            if (predefinedEquipmentStacks.Count == 0) return;

            if (stacks.Count > 0)
            {
                Debug.LogWarning(PrefixLogMessage($"There exists stacks already even though we should generate them"));
            }

            var i = stacks.Count;
            foreach (var eq in predefinedEquipmentStacks)
            {
                var stack = new InventoryStack(i, eq, predefinedAreSingleItem);
                RegisterStackEvents(stack);
                stacks.Add(stack);
                i++;
            }

            Capacity = stacks.Count;
        }

        public void OnLoad(InventorySave<StackedItemInfo> save)
        {
            var disposed = ItemDisposal.InstanceOrCreate().GetDisposed(FullId).ToList();
            var recylcer = RecycleBin.InstanceOrCreate();

            // 1. Dispose of already disposed items
            foreach (var item in stacks.SelectMany(stack => stack.Items).Where(item => disposed.Any(itemId => item.Id == itemId)))
            {
                Remove(item);
                item.gameObject.SetActive(false);
                item.transform.SetParent(ItemDisposal.instance.transform);
            }

            if (save == null)
            {
                Debug.LogWarning(PrefixLogMessage("No save info exists for me"));
                return;
            }

            // 2. Recycle things no longer here for safety
            foreach (var item in stacks.SelectMany(stack => stack.Items).Where(item => !save.items.Any(info => info.Equals(item))))
            {
                Remove(item);
                recylcer.Add(item);
                if (item.WorldRoot != null) item.WorldRoot.gameObject.SetActive(false);
            }

            // 3. Claim or create those items that weren't originally here
            foreach (var info in save.items.Where(info => info.OriginId != FullId))
            {
                if (recylcer.Remove(info.ItemId, info.OriginId, out AbsItem item))
                {
                    Add(item);
                }
                else
                {
                    var originalInventory = GetByFullId(info.OriginId);
                    if (originalInventory == null)
                    {
                        if (SimpleItemFactory.InstanceOrCreate().Create(info.ItemId, info.OriginId, out item))
                        {
                            Add(item);
                        }
                        else
                        {
                            Debug.LogError(PrefixLogMessage($"{info.ItemId} could not be recoverd from {info.OriginId} because inventory not known in scene and item not known by factory"));
                        }
                    }
                    else
                    {
                        if (originalInventory.Remove(info.ItemId, out item))
                        {
                            Add(item);
                        }
                        else
                        {
                            Debug.LogError(PrefixLogMessage($"{info.ItemId} should have existed in {originalInventory.FullId} but it refused to give us one, so will try to make new one"));
                            if (SimpleItemFactory.InstanceOrCreate().Create(info.ItemId, info.OriginId, out item))
                            {
                                Add(item);
                            }
                            else
                            {
                                Debug.LogError(PrefixLogMessage($"{info.ItemId} could not be created by factory"));
                            }
                        }
                    }
                }
            }

            // 4. Order inventory according to save
            for (int stackIdx = 0, l = stacks.Count; stackIdx < l; stackIdx++)
            {
                var stack = stacks[stackIdx];

                foreach (var item in stack.Items.ToList())
                {
                    var info = save.items.FirstOrDefault(info => info.Equals(item));
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
            }
        }

        public IEnumerable<StackedItemInfo> Save() =>
            stacks.SelectMany((stack, idx) => stack.Items.Select(item => new StackedItemInfo(item, idx)));
    }
}
