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

        public void LoadFromSave(IEnumerable<ItemOrigin> disposed)
        {
            items.Clear();
            items.AddRange(disposed);
        }

        public List<ItemOrigin> SaveState() => items.ToList();
    }
}
