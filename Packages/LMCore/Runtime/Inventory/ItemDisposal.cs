using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.Inventory
{

    [System.Serializable]
    public struct ItemOrigin
    {
        public string ItemId;
        public string OriginId;
    }

    public class ItemDisposal : Singleton<ItemDisposal> { 

        List<ItemOrigin> items = new List<ItemOrigin>();

        public void Dispose(string itemId, string originId)
        {
            items.Add(new ItemOrigin { ItemId = itemId, OriginId = originId });
        }

        public IEnumerable<string> GetDisposed(string originId) =>
            items.Where(i => i.OriginId == originId).Select(i => i.ItemId);

        // TODO: Add loading and saving features
    }
}
