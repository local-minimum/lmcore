using LMCore.AbstractClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.Inventory
{

    [Serializable]
    public class ItemOrigin: ItemInfo
    {
        public override bool Equals(object obj)
        {
            if (obj is ItemOrigin)
            {
                var other = (ItemOrigin)obj;
                return ItemId == other.ItemId && OriginId == other.OriginId;
            } else if (obj is AbsItem)
            {
                var other = (AbsItem)obj;
                return ItemId == other.Id && OriginId == other.Origin;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode() => HashCode.Combine(ItemId, OriginId);
    }

    public class ItemDisposal : Singleton<ItemDisposal, ItemDisposal> { 

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
