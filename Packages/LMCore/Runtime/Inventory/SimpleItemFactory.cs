using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    public class SimpleItemFactory : AbsItemFactory
    {
        [System.Serializable]
        struct ItemInstruction
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
                item = null;
                return false;
            }

            var instruction = Instructions.First(i => i.ItemId == itemId);

            var simpleItem = Instantiate(instruction.Prefab);
            simpleItem.Configure(itemId, originId, instruction.StackSize);

            item = simpleItem;
            return true;
        }
    }
}
