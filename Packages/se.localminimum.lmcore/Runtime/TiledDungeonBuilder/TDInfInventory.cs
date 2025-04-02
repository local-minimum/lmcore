using LMCore.Inventory;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDInfInventory : SimpleInventory
    {
        public new bool Full => false;

        public override void Configure(string id, AbsInventory parent, int capacity)
        {
            base.Configure(id, parent, Capacity);
        }

        private InventoryStack ExtendStacksToIncludeId(int stackId)
        {
            var nStacks = stacks.Count;
            InventoryStack stack = null;
            if (stackId >= nStacks)
            {
                var neededNew = stackId - nStacks + 1;
                for (int i = 0; i < neededNew; i++)
                {
                    stack = new InventoryStack(nStacks);
                    RegisterStackEvents(stack);
                    stacks.Add(stack);
                    nStacks++;
                }
            }
            else
            {
                stack = stacks[stackId];
            }

            if (nStacks != Capacity)
            {
                Capacity = nStacks;
            }

            return stack;
        }

        protected new bool AddToStack(int stackId, AbsItem item)
        {
            ExtendStacksToIncludeId(stackId);
            return base.AddToStack(stackId, item);
        }

        public override bool AddItemsToEmptyStack(int stackIdx, IEnumerable<AbsItem> items)
        {
            var stack = ExtendStacksToIncludeId(stackIdx);

            if (!ItemsCanBeStacked(items)) return false;

            if (!AddItemsToStack(stack, items)) return false;

            return true;
        }

        protected new bool AddToFirstMatchingStack(AbsItem item, out InventoryStack stack)
        {
            if (!base.AddToFirstMatchingStack(item, out stack))
            {
                stack = ExtendStacksToIncludeId(stacks.Count);

                if (stack == null) return false;

                return AddToStack(stack.Index, item);
            }

            return true;
        }

        // TODO Add load stuff here
        public IEnumerable<StackedItemInfo> Save() =>
            stacks.SelectMany((stack, idx) => stack.Items.Select(item => new StackedItemInfo(item, idx)));

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

            Capacity = stacks.Count;
        }
    }
}
