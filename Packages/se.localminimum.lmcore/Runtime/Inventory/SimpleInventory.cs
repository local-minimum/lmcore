using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    public class SimpleInventory : AbsInventory
    {
        [SerializeField]
        private string _DisplayName;
        public override string DisplayName => _DisplayName;

        [SerializeField, HideInInspector]
        private string _Id;
        public override string Id => _Id;

        public override string FullId => Parent == null ? Id : $"{Parent.FullId}-{Id}";


        [SerializeField, HideInInspector]
        protected List<InventoryStack> stacks = new List<InventoryStack>();

        public override IEnumerable<AbsItem> Items => stacks.SelectMany(s => s.Items);
        public override bool Empty => !Items.Any();

        /// <summary>
        /// Lists all stacks (but may hide stacks outside of capacity)
        /// </summary>
        public override IEnumerable<InventoryStack> Stacks
        {
            get
            {
                // If an inventory has a dynamic size, it is the responsibility
                // of that inventory to resize the capacity before enumerating 
                // stacks.
                for (int i = 0, nStacks = stacks.Count; i < Capacity; i++)
                {
                    InventoryStack stack = i < nStacks ? stacks[i] : null;

                    if (stack == null)
                    {
                        stack = new InventoryStack(i);
                        RegisterStackEvents(stack);
                        stacks.Add(stack);
                    }

                    yield return stack;
                }
            }
        }

        public InventoryStack FirstByEquipment(EquipmentType equipment) =>
            stacks.FirstOrDefault(s => s.Equipment == equipment);

        public override int Used => stacks.Count;

        public override void Configure(string id, AbsInventory parent, int capacity)
        {
            _Id = id;
            Capacity = capacity;
            Parent = parent;
        }

        public override void Configure(string id, AbsInventory parent = null)
        {
            _Id = id;
            Parent = parent;
        }

        [ContextMenu("Info")]
        public void Info()
        {
            Debug.Log($"Inventory ({name}) size {Capacity} {(stacks.Count == 0 ? "is empty" : $"has stacks {string.Join(", ", stacks)}")}");
        }

        public override string ToString() =>
            $"<Inventory {name} size {Capacity}: {(stacks.Count == 0 ? "[Empty]" : string.Join(", ", stacks))}>";

        public bool Full => stacks.Count >= Capacity && stacks.All(stack => stack.Full);

        private void Start()
        {
            foreach (var stack in stacks)
            {
                RegisterStackEvents(stack);
            }
        }

        protected bool AddToStack(int stackIdx, AbsItem item)
        {
            var nStacks = stacks.Count;
            if (stackIdx < nStacks)
            {
                var stack = stacks[stackIdx];
                if (stack.Add(item))
                {
                    EmitAdded(item, stack);
                }
                return false;
            }

            if (stackIdx < Capacity)
            {
                InventoryStack stack;
                while (nStacks <= stackIdx)
                {
                    stack = new InventoryStack(nStacks);
                    RegisterStackEvents(stack);
                    stacks.Add(stack);
                    nStacks++;
                }

                stack = stacks[stackIdx];
                if (stack.Add(item))
                {
                    EmitAdded(item, stack);
                    return true;
                }

                return false;
            }

            return false;
        }

        HashSet<InventoryStack> registeredStacks = new HashSet<InventoryStack>();
        protected void RegisterStackEvents(InventoryStack stack)
        {
            if (registeredStacks.Contains(stack)) return;

            stack.OnGainItem += (item) =>
            {
                Debug.Log(PrefixLogMessage($"{item} added to {stack} in {name}"));
                item.transform.SetParent(transform);
                EmitAdded(item, stack);
            };
            stack.OnRemoveItem += (item) => EmitRemoved(item, stack);

            registeredStacks.Add(stack);
        }

        protected bool ItemsCanBeStacked(IEnumerable<AbsItem> items)
        {
            var firstItem = items.FirstOrDefault();
            var itemId = firstItem?.Id;

            return items.All(i => i.Id == itemId);
        }

        protected bool AddItemsToStack(InventoryStack stack, IEnumerable<AbsItem> items)
        {
            if (stack == null) return false;

            foreach (var item in items)
            {
                if (!stack.Add(item)) return false;
                EmitAdded(item, stack);
                item.transform.SetParent(transform);
            }

            return true;
        }

        override public bool AddItemsToEmptyStack(int stackIdx, IEnumerable<AbsItem> items)
        {
            if (stackIdx >= Capacity || !ItemsCanBeStacked(items)) return false;

            InventoryStack stack = null;
            if (stackIdx < stacks.Count)
            {
                stack = stacks[stackIdx];
                if (!stack.Empty) return false;

            }

            bool addStack = false;
            if (stack == null)
            {
                stack = new InventoryStack(stacks.Count);
                RegisterStackEvents(stack);
                addStack = true;
            }

            if (!AddItemsToStack(stack, items)) return false;

            if (addStack)
            {
                while (stackIdx < stacks.Count)
                {
                    var otherStack = new InventoryStack(stacks.Count);
                    RegisterStackEvents(otherStack);
                    stacks.Add(otherStack);
                }

                stacks.Add(stack);
            }

            return true;
        }

        protected bool AddToFirstMatchingStack(AbsItem item, out InventoryStack stack)
        {
            foreach (var s in stacks)
            {
                if (s.Empty || (s.ItemId == item.Id && !s.Full))
                {
                    if (s.Add(item))
                    {
                        item.transform.SetParent(transform);
                        stack = s;
                        EmitAdded(item, stack);
                        return true;
                    }
                }
            }

            if (stacks.Count < Capacity)
            {
                var newStack = new InventoryStack(stacks.Count, item);
                RegisterStackEvents(newStack);
                stacks.Add(newStack);
                stack = newStack;
                item.transform.SetParent(transform);
                EmitAdded(item, stack);
                return true;
            }

            stack = null;
            return false;
        }

        protected string PrefixLogMessage(string message) => $"Inventory {FullId}: {message}";
        public override bool Add(AbsItem item)
        {
            if (Full)
            {
                Debug.Log(PrefixLogMessage($"failed to add {item.FullId}, inventory is full"));
                return false;
            }

            if (!AddToFirstMatchingStack(item, out var stack))
            {
                Debug.LogWarning(PrefixLogMessage($"failed to add {item.FullId}, no place in any stack"));
                return false;
            }

            if (item.WorldRoot)
            {
                if (item.WorldRoot.transform.parent != item.transform)
                {
                    item.WorldRoot.transform.SetParent(item.WorldRoot);
                    item.WorldRoot.gameObject.SetActive(false);
                }
            }

            EmitAdded(item, stack);

            Debug.Log(PrefixLogMessage($"{Used}/{Capacity} after adding {item.Id} (full count {Items.Count()})"));

            return true;
        }

        public override bool Consume(string itemId, out string origin)
        {
            if (Remove(itemId, out var item))
            {
                origin = item.Origin;

                return true;
            }

            origin = null;
            return false;
        }

        public override bool HasItem(string itemId) =>
            stacks.Any(stack => stack.ItemId == itemId);

        public override bool Remove(string itemId, out AbsItem item)
        {
            if (!HasItem(itemId))
            {
                item = null;
                return false;
            }

            foreach (var stack in stacks)
            {
                if (stack.ItemId == itemId)
                {
                    if (stack.Remove(itemId, out item))
                    {
                        EmitRemoved(item, stack);
                        return true;
                    }
                }
            }

            Debug.LogError(PrefixLogMessage($"Claimed to have {itemId} but non could be removed"));

            item = null;
            return false;
        }

        public override bool Remove(AbsItem item)
        {

            foreach (var stack in stacks)
            {
                if (stack.Remove(item))
                {
                    EmitRemoved(item, stack);
                    return true;
                }
            }

            return false;
        }
    }
}
