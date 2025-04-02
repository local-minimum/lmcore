using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{

    public delegate void ItemChangeEvent(AbsItem item, InventoryStack stack);
    public delegate void ChangedCapacityEvent(AbsInventory inventory);

    // TODO: Add IOnLoadSave and make a DungeonLevelInventory too... Probably want an item recycle-bin too...
    public abstract class AbsInventory : MonoBehaviour
    {
        public event ChangedCapacityEvent OnChangedCapacity;
        public event ItemChangeEvent OnAddItem;
        public event ItemChangeEvent OnRemoveItem;

        [HideInInspector]
        public AbsInventory Parent;

        public abstract string Id { get; }
        public abstract string FullId { get; }

        public abstract string DisplayName { get; }

        [SerializeField, HideInInspector]
        int _Capacity;
        /// <summary>
        /// How many stacks/slots the inventory can have
        /// </summary>
        public int Capacity
        {
            get => _Capacity;
            protected set
            {
                _Capacity = value;
                OnChangedCapacity?.Invoke(this);
            }
        }

        public abstract int Used { get; }

        public abstract bool HasItem(string itemId);

        public abstract bool Consume(string itemId, out string origin);

        public abstract bool Remove(string itemId, out AbsItem item);

        public abstract bool Remove(AbsItem item);

        public abstract bool Add(AbsItem item);
        public abstract bool AddItemsToEmptyStack(int stackIdx, IEnumerable<AbsItem> items);

        protected void EmitAdded(AbsItem item, InventoryStack stack) => OnAddItem?.Invoke(item, stack);

        protected void EmitRemoved(AbsItem item, InventoryStack stack) => OnRemoveItem?.Invoke(item, stack);

        /// <summary>
        /// All items in the inventory in no particular order
        /// </summary>
        public abstract IEnumerable<AbsItem> Items { get; }

        /// <summary>
        /// If there are any items in the inventory
        /// </summary>
        public abstract bool Empty { get; }

        /// <summary>
        /// All inventory slots/stacks of the inventory, empty or not
        /// </summary>
        public abstract IEnumerable<InventoryStack> Stacks { get; }

        public abstract void Configure(string id, AbsInventory parent, int capacity);
        public abstract void Configure(string id, AbsInventory parent = null);

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
