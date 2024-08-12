using System.Collections;
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

        public override bool Create(string itemId, string originId, out AbsItem item)
        {
            if (!Instructions.Any(i => i.ItemId == itemId))
            {
                Debug.LogWarning($"No instruction found for '{itemId}'");
                item = null;
                return false;
            }

            var instruction = Instructions.First(i => i.ItemId == itemId);

            item = Instantiate(instruction.Prefab);

            ((SimpleItem) item).Configure(itemId, originId, instruction.StackSize);

            if (item.WorldRoot != null)
            {
                item.WorldRoot.gameObject.SetActive(false);
            }
            if (item.UIRoot != null)
            {
                item.UIRoot.gameObject.SetActive(false);
            }

            return true;
        }
    }
}
