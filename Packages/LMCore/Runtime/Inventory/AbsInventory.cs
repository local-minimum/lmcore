using Codice.CM.Common.Purge;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{

    public delegate void ItemChangeEvent(AbsItem item);

    public abstract class AbsInventory : MonoBehaviour
    {
        public event ItemChangeEvent OnAddItem;
        public event ItemChangeEvent OnRemoveItem;

        [HideInInspector]
        public AbsInventory Parent;

        public abstract string Id { get; }
        public abstract string FullId { get; }

        public abstract int Capacity { get; }
        public abstract int Used { get; }

        public abstract bool HasItem(string itemId);

        public abstract bool Consume(string itemId, out string origin);

        public abstract bool Remove(string itemId, out AbsItem item);

        public abstract bool Remove(AbsItem item);

        public abstract bool Add(AbsItem item);

        protected void EmitAdded(AbsItem item) => OnAddItem?.Invoke(item);

        protected void EmitRemoved(AbsItem item) => OnRemoveItem?.Invoke(item); 

        public abstract IEnumerable<AbsItem> Items { get; }

        public abstract void Configure(string id, AbsInventory parent, int capacity);

        static List<AbsInventory> Inventories = new List<AbsInventory>();

        public static AbsInventory GetByFullId(string fullId) =>
            Inventories.FirstOrDefault(i => i.FullId == fullId);


        private void Awake()
        {
            Inventories.Add(this);
        }

        private void OnDestroy()
        {
            Inventories.Remove(this);
        }
    }
}
