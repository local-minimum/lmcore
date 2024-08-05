using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    public class SimpleInventory : AbsInventory
    {       
        [SerializeField, HideInInspector]
        private string _Id;
        public override string Id => _Id; 

        public override string FullId => Parent == null ? Id : $"{Parent.FullId}-{Id}";

        [SerializeField, HideInInspector]
        int _Capacity;
        public override int Capacity => _Capacity;

        [SerializeField, HideInInspector]
        List<InventoryStack> stacks = new List<InventoryStack>();

        public override IEnumerable<AbsItem> Items => stacks.SelectMany(s => s.Items);

        public override int Used => stacks.Count;

        public override void Configure(string id, AbsInventory parent, int capacity)
        {
            _Id = id;
            _Capacity = capacity;
            Parent = parent;
        }

        public bool Full => stacks.Count >= Capacity && stacks.All(stack => stack.Full);

        private bool AddToStack(AbsItem item)
        {
            foreach (var stack in stacks)
            {
                if (stack.Empty || stack.Id == item.Id && !stack.Full)
                {
                    stack.Add(item);
                    return true;
                }
            }

            if (stacks.Count < Capacity)
            {
                var newStack = new InventoryStack(item);
                stacks.Add(newStack);
                return true;
            }

            return false;
        }

        public override bool Add(AbsItem item)
        {
            if (Full)
            {
                Debug.Log($"Inventory {name}: failed to add {item.FullId}, inventory is full");
                return false;
            }

            if (!AddToStack(item))
            {
                Debug.LogWarning($"Inventory {name}: failed to add {item.FullId}, no place in any stack");
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

            EmitAdded(item);

            Debug.Log($"Inventory {name}: {Used}/{Capacity} after adding {item.Id} (full count {Items.Count()})");
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
            stacks.Any(stack => stack.Id == itemId);

        public override bool Remove(string itemId, out AbsItem item)
        {
            if (!HasItem(itemId))
            {
                item = null;
                return false;
            }

            foreach (var stack in stacks)
            {
                if (stack.Id == itemId)
                {
                    if (stack.Remove(itemId, out item))
                    {
                        EmitRemoved(item);
                        return true;
                    }
                }
            }

            Debug.LogError($"Inventory {name}: Claimed to have {itemId} but non could be removed");

            item = null;
            return false;
        }

        public override bool Remove(AbsItem item)
        {

            foreach (var stack in stacks)
            {
                if (stack.Remove(item))
                {
                    EmitRemoved(item);
                    return true;
                }
            }

            return false;
        }

        // TODO: Add saving and loading
    }
}
