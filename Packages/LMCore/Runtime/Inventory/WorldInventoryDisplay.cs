using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    public class WorldInventoryDisplay : MonoBehaviour
    {
        [SerializeField]
        Transform target;

        [SerializeField, HideInInspector]
        HashSet<AbsItem> items = new ();

        IEnumerable<AbsInventory> inventories => GetComponentsInChildren<AbsInventory>(true);

        Transform Target => target == null ? transform : target;

        private void OnEnable()
        {
            foreach (var inventory in inventories)
            {
                inventory.OnAddItem += Inventory_OnAddItem;
                inventory.OnRemoveItem += Inventory_OnRemoveItem;
            }
        }

        private void OnDisable()
        {
            foreach (var inventory in inventories)
            {
                inventory.OnAddItem -= Inventory_OnAddItem;
                inventory.OnRemoveItem -= Inventory_OnRemoveItem;
            }
        }

        private void Inventory_OnAddItem(AbsItem item) => DisplayItem(item);

        private void Inventory_OnRemoveItem(AbsItem item) => RemoveItem(item);

        void DisplayItem(AbsItem item)
        {
            if (item.UIRoot != null)
            {
                item.UIRoot.gameObject.SetActive(false);
            }

            if (item.WorldRoot != null)
            {
                item.WorldRoot.SetParent(Target);

                item.WorldRoot.transform.localPosition = Vector3.zero;
                item.WorldRoot.transform.localRotation = Quaternion.identity;

                item.WorldRoot.gameObject.SetActive(true);
            }

            items.Add(item);
        }

        void RemoveItem(AbsItem item)
        {
            Debug.Log($"Word Inventory Display: Removing {item.FullId}");
            if (item.WorldRoot != null && item.WorldRoot.transform.parent == Target)
            {
                item.WorldRoot.gameObject.SetActive(false);
                item.WorldRoot.SetParent(item.transform);
                item.WorldRoot.localPosition = Vector3.zero;
            }

            items.Remove(item);
        }

        public void Sync()
        {
            HashSet<AbsItem> current = new HashSet<AbsItem>();

            foreach (var item in inventories.SelectMany(inv => inv.Items))
            {
                current.Add(item);

                if (items.Contains(item)) continue;

                DisplayItem(item);
            }

            var removals = items.Except(current).ToList();
            foreach (var item in removals)
            {
                RemoveItem(item);
            }
        }
    }
}
