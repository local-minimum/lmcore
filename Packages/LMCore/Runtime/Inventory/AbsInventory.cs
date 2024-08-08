using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{

    public delegate void ItemChangeEvent(AbsItem item);

    // TODO: Add IOnLoadSave and make a DungeonLevelInventory too... Probably want an item recycle-bin too...
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

        static List<AbsInventory> AllInventories = new List<AbsInventory>();

        public static AbsInventory GetByFullId(string fullId) =>
            AllInventories.FirstOrDefault(i => i.FullId == fullId);


        private void Awake()
        {
            AllInventories.Add(this);
        }

        private void OnDestroy()
        {
            AllInventories.Remove(this);
        }
    }
}
