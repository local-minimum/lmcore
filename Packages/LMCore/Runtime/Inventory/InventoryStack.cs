using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    [System.Serializable]
    public class InventoryStack
    {
        [SerializeField]
        List<AbsItem> items = new List<AbsItem>();

        public string Id => Empty ? null : items[0].Id;
        public bool Empty => items.Count == 0;
        public bool Full => !Empty && (!items[0].Stackable || items.Count >= items[0].StackSizeLimit);

        public override string ToString()
        {
            if (Empty) return "[EMPTY]";

            return $"{items.Count} {Id}{(Full ? " (Full)" : "")}";
        }
        public IEnumerable<AbsItem> Items => items;

        public InventoryStack() { }
        public InventoryStack(AbsItem item) { items.Add(item); }

        public bool Add(AbsItem item)
        {
            if (!Empty)
            {
                if (Id == item.Id)
                {
                    if (Full)
                    {
                        Debug.Log($"Stack of {item.Id} is full");
                        return false;
                    }

                    items.Add(item);
                    return true;
                } else
                {
                    Debug.LogError($"Stack of {Id} can't take a {item.Id}");
                    return false;
                }
            } else
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
                Debug.LogWarning($"Stack of {Id} doesn't allow removal by empty id");
                removedItem = null;
                return false;
            }

            if (id == Id)
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
                Debug.LogWarning($"Stack of {Id} doesn't allow removal by empty id");
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
    }
}
