using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    public delegate void StackChangeEvent(AbsItem item);

    [System.Serializable]
    public class InventoryStack
    {
        public event StackChangeEvent OnGainItem;
        public event StackChangeEvent OnRemoveItem;

        [SerializeField]
        List<AbsItem> items = new List<AbsItem>();

        [SerializeField]
        int index;

        [SerializeField]
        EquipmentType equipmentType = EquipmentType.None;

        /// <summary>
        /// The type of equipment that the stack can hold
        /// </summary>
        public EquipmentType Equipment => equipmentType;

        [SerializeField]
        bool SingleItemStack;

        public int Index => index;

        /// <summary>
        /// Item id of stack contents
        /// </summary>
        public string ItemId => Empty ? null : items[0].Id;

        public string ItemName => Empty ? null : items[0].Name;

        /// <summary>
        /// Item type of the stack
        /// </summary>
        public ItemType ItemType => Empty ? ItemType.Nothing : items[0].Type;

        /// <summary>
        /// Equipment type of the stack
        /// </summary>
        public EquipmentType EquipmentType => Empty ? EquipmentType.None : items[0].Equipment;

        /// <summary>
        /// Nothing in the stack
        /// </summary>
        public bool Empty => items.Count == 0;

        /// <summary>
        /// No more space in the stack
        /// </summary>
        public bool Full => !Empty && (
            SingleItemStack ?
            items.Count > 0 :
            (!items[0].Stackable || items.Count >= items[0].StackSizeLimit));

        /// <summary>
        /// Number of items in stack
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// Number of item copies that can be added
        /// </summary>
        public int RemaingingStackSpace =>
            Empty ? 0 : (SingleItemStack ? 1 - items.Count : items[0].StackSizeLimit - Count);

        public override string ToString()
        {
            if (Empty) return $"[{Index}/{equipmentType}: EMPTY]";

            return $"[{Index}/{equipmentType}: {items.Count} {ItemId}{(Full ? " (Full)" : "")}]";
        }
        public IEnumerable<AbsItem> Items => items;

        public Sprite UISprite => Items.FirstOrDefault()?.UISprite;

        public InventoryStack(
            int index,
            EquipmentType equipment = EquipmentType.None,
            bool singleItemStack = false
        )
        {
            this.index = index;
            equipmentType = equipment;
            SingleItemStack = singleItemStack;
        }

        public InventoryStack(
            int index,
            AbsItem item,
            EquipmentType equipment = EquipmentType.None,
            bool singleItemStack = false
        )
        {
            this.index = index;
            items.Add(item);
            equipmentType = equipment;
            SingleItemStack = singleItemStack;
        }

        bool ValidEquipment(AbsItem item)
        {
            // We can always be empty
            if (item == null) return true;

            // We don't have a limitation
            if (equipmentType == EquipmentType.None) return true;

            return equipmentType == item.Equipment;
        }

        bool ValidEquipment(InventoryStack stack) =>
            ValidEquipment(stack.items.FirstOrDefault());

        public bool Add(AbsItem item)
        {
            if (!Empty)
            {
                if (!ValidEquipment(item))
                {
                    Debug.Log($"Stack can't take {item} because not of equipment type {equipmentType}");
                    return false;
                }
                else if (ItemId == item.Id)
                {
                    if (Full)
                    {
                        Debug.Log($"Stack of {item.Id} is full");
                        return false;
                    }

                    items.Add(item);
                    return true;
                }
                else
                {
                    Debug.LogError($"Stack of {ItemId} can't take a {item.Id}");
                    return false;
                }
            }
            else
            {
                items.Add(item);
                return true;
            }
        }

        public bool Remove(AbsItem item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                return true;
            }
            return false;
        }

        public bool Remove(out AbsItem removedItem)
        {
            if (Empty)
            {
                removedItem = null;
                return false;
            }

            removedItem = items.Last();
            items.Remove(removedItem);
            return true;
        }

        public bool Remove(string id, out AbsItem removedItem)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"Stack of {ItemId} doesn't allow removal by empty id");
                removedItem = null;
                return false;
            }

            if (id == ItemId)
            {
                return Remove(out removedItem);
            }

            removedItem = null;
            return false;
        }

        public bool RemoveByFullId(string fullId, out AbsItem removedItem)
        {
            if (string.IsNullOrEmpty(fullId))
            {
                Debug.LogWarning($"Stack of {ItemId} doesn't allow removal by empty id");
                removedItem = null;
                return false;
            }

            foreach (AbsItem item in items)
            {
                if (item.FullId == fullId)
                {
                    items.Remove(item);
                    removedItem = item;
                    return true;
                }
            }

            removedItem = null;
            return false;
        }

        /// <summary>
        /// If part or all of the content of the other can be transferred to this stack
        /// </summary>
        public bool Mergeable(InventoryStack other) =>
            other != this &&
            ValidEquipment(other) &&
            (ItemId == null || ItemId == other.ItemId);

        /// <summary>
        /// Transfers as much as possible from the other stack to this stack
        /// </summary>
        public void MergeContent(InventoryStack other)
        {
            if (!ValidEquipment(other))
            {
                return;
            }

            if (ItemId == null)
            {
                if (SingleItemStack)
                {
                    var item = other.items.First();

                    other.items.RemoveAt(0);
                    other.OnRemoveItem?.Invoke(item);

                    items.Add(item);
                    OnGainItem?.Invoke(item);
                }
                else
                {
                    var transferred = other.items.ToList();
                    other.items.Clear();
                    items.AddRange(transferred);
                    foreach (var item in transferred)
                    {
                        other.OnRemoveItem?.Invoke(item);
                        OnGainItem?.Invoke(item);
                    }
                }
                return;
            }

            if (ItemId != other.ItemId)
            {
                throw new System.ArgumentException($"Incompateble stacks: {ItemId} is not the same as {other.ItemId}");
            }

            var claimable = RemaingingStackSpace;
            if (claimable > 0)
            {
                var transferred = other.items.Take(claimable).ToList();
                items.AddRange(transferred);
                other.items = other.items.Skip(claimable).ToList();
                foreach (var item in transferred)
                {
                    other.OnRemoveItem?.Invoke(item);
                    OnGainItem?.Invoke(item);
                }
            }
        }

        public bool SwapContent(InventoryStack other)
        {
            if (other == this) return false;

            var myPreviousItems = items.ToList();

            var iCanAccept = ValidEquipment(other) && (!SingleItemStack || other.Count == 1);
            var theyCanAccept = other.ValidEquipment(this) && (!other.SingleItemStack || Count == 1);

            if (!iCanAccept || !theyCanAccept)
            {
                Debug.LogWarning($"{this} and {other} cannot swap content");
                return false;
            }

            items.Clear();
            items.AddRange(other.items);
            foreach (var item in myPreviousItems)
            {
                OnRemoveItem?.Invoke(item);
            }
            foreach (var item in items)
            {
                OnGainItem?.Invoke(item);
            }


            other.items.Clear();
            other.items.AddRange(myPreviousItems);
            foreach (var item in items)
            {
                other.OnRemoveItem?.Invoke(item);
            }
            foreach (var item in other.items)
            {
                other.OnGainItem?.Invoke(item);
            }

            return true;
        }

        public void Clear()
        {
            var prevItems = items.ToList();
            items.Clear();
            foreach (var item in prevItems)
            {
                OnRemoveItem?.Invoke(item);
            }
        }
    }
}
