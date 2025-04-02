using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    public class SimpleItemFactory : AbsItemFactory<SimpleItemFactory>
    {
        [System.Serializable]
        class ItemInstruction
        {
            public string ItemId;
            public SimpleItem Prefab;
            public int StackSize;
        }

        [SerializeField]
        List<ItemInstruction> Instructions;

        bool InstantiateItem(string itemId, string originId, out AbsItem item)
        {
            if (!Instructions.Any(i => i.ItemId == itemId))
            {
                Debug.LogWarning($"No instruction found for '{itemId}'");
                item = null;
                return false;
            }

            var instruction = Instructions.First(i => i.ItemId == itemId);

            item = Instantiate(instruction.Prefab);

            ((SimpleItem)item).Configure(itemId, originId, instruction.StackSize);
            return true;
        }

        bool ReuseItem(string itemId, string originId, out AbsItem item)
        {
            item = recycled.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return false;

            recycled.Remove(item);
            item.transform.SetParent(null);
            item.gameObject.SetActive(true);

            ((SimpleItem)item).Configure(itemId, originId, item.StackSizeLimit);
            return true;
        }

        bool GetItem(string itemId, string originId, out AbsItem item)
        {
            if (ReuseItem(itemId, originId, out item)) return true;
            return InstantiateItem(itemId, originId, out item);
        }

        public override bool Create(string itemId, string originId, out AbsItem item)
        {
            if (!GetItem(itemId, originId, out item)) return false;


            if (item.WorldRoot != null)
            {
                item.WorldRoot.gameObject.SetActive(false);
            }

            Debug.Log($"Spawining {item} as response to request id '{itemId}' to '{originId}'");
            return true;
        }

        List<AbsItem> recycled = new List<AbsItem>();

        public override void Recycle(AbsItem item)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(transform);
            recycled.Add(item);
        }
    }
}
