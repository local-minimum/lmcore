using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    public class WorldInventoryDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("Should be empty nodes without any content in them")]
        List<Transform> displayLocations = new List<Transform>();

        public IEnumerable<Transform> Locations => displayLocations;

        public int LocationCount => displayLocations.Count;

        [SerializeField, HideInInspector]
        List<AbsItem> displayedItems = new();

        IEnumerable<AbsInventory> inventories => GetComponentsInChildren<AbsInventory>(true);

        bool CurrentlyDisplayed(AbsItem item)
        {
            if (item.WorldRoot == null) return false;

            return displayLocations.IndexOf(item.WorldRoot.parent) != -1;
        }

        Transform GetLocationParent(AbsItem item, int slotIndex)
        {
            var locations = displayLocations.Count;
            if (locations == 0) return transform;

            if (item.WorldRoot != null)
            {
                var currentIdx = displayLocations.IndexOf(item.WorldRoot.parent);
                if (currentIdx != -1)
                {
                    return displayLocations[currentIdx];
                }
            }

            return displayLocations[slotIndex % locations];
        }

        private void OnEnable()
        {
            foreach (var inventory in inventories)
            {
                // Debug.Log($"Word Inventory Display: Hooking up to {inventory}");
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

        private void Inventory_OnAddItem(AbsItem item, InventoryStack stack) =>
            DisplayItem(item, stack.Index);

        private void Inventory_OnRemoveItem(AbsItem item, InventoryStack stack) =>
            RemoveItem(item);

        void DisplayItem(AbsItem item, int slotIndex)
        {
            Debug.Log($"Word Inventory Display: Adding {item} (Has world: {item.WorldRoot != null})");
            if (item.WorldRoot != null)
            {
                item.WorldRoot.SetParent(GetLocationParent(item, slotIndex));

                item.WorldRoot.transform.localPosition = Vector3.zero;
                item.WorldRoot.transform.localRotation = Quaternion.identity;

                item.WorldRoot.gameObject.SetActive(true);

                displayedItems.Add(item);
            }

        }

        void RemoveItem(AbsItem item)
        {
            Debug.Log($"Word Inventory Display: Removing {item.FullId}");
            if (item.WorldRoot != null && CurrentlyDisplayed(item))
            {
                item.WorldRoot.gameObject.SetActive(false);
                item.WorldRoot.SetParent(item.transform);
                item.WorldRoot.localPosition = Vector3.zero;
            }

            displayedItems.Remove(item);
        }

        public void Sync()
        {
            HashSet<AbsItem> current = new HashSet<AbsItem>();

            foreach (var stack in inventories.SelectMany(inv => inv.Stacks))
            {
                foreach (var item in stack.Items)
                {
                    current.Add(item);

                    if (displayedItems.Contains(item)) continue;

                    DisplayItem(item, stack.Index);
                }
            }

            var removals = displayedItems.Except(current).ToList();
            foreach (var item in removals)
            {
                RemoveItem(item);
            }
        }
    }
}
