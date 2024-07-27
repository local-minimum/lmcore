using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    public class SimpleInventory : AbsInventory
    {       
        [SerializeField]
        private string _Id;
        public override string Id => _Id; 

        public override string FullId => Parent == null ? Id : $"{Parent.FullId}-{Id}";

        [SerializeField]
        int _Capacity;
        public override int Capacity => _Capacity;

        [SerializeField]
        List<AbsItem> items = new List<AbsItem>();

        public override int Used => items.Count;

        public void Configure(string id, int capacity)
        {
            _Id = id;
            _Capacity = capacity;
        }

        public override bool Add(AbsItem item)
        {
            if (items.Count >= Capacity) return false;

            items.Add(item);

            EmitAdded(item);

            return true;
        }

        public override bool Consume(string itemId, out string origin)
        {
            if (!items.Any(i => i.Id == itemId))
            {
                origin = null;
                return false;
            }

            var item = items.First(i => i.Id == itemId);
            origin = item.Origin;

            EmitRemoved(item);

            return true;
        }

        public override bool HasItem(string itemId) =>
            items.Any(i => i.Id == itemId);

        public override bool Remove(string itemId, out AbsItem item)
        {
            if (!items.Any(i => i.Id == itemId))
            {
                item = null;
                return false;
            }

            item = items.First(i => i.Id == itemId);

            EmitRemoved(item);

            return true;
        }

        // TODO: Add saving and loading
    }
}
