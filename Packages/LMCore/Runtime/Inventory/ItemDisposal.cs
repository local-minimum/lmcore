using LMCore.AbstractClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.Inventory
{

    [Serializable]
    public struct ItemOrigin
    {
        public string ItemId;
        public string OriginId;

        public override bool Equals(object obj)
        {
            if ( obj is ItemOrigin)
            {
                var other = (ItemOrigin)obj;
                return ItemId == other.ItemId && OriginId == other.OriginId;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode() => HashCode.Combine(ItemId, OriginId);
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
