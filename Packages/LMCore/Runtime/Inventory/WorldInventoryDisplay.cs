using UnityEngine;

namespace LMCore.Inventory
{
    public class WorldInventoryDisplay : MonoBehaviour
    {
        [SerializeField]
        Transform target;

        private void OnEnable()
        {
            foreach (var inventory in GetComponentsInChildren<AbsInventory>(true))
            {
                inventory.OnAddItem += Inventory_OnAddItem;
                inventory.OnRemoveItem += Inventory_OnRemoveItem;
            }
        }

        private void OnDisable()
        {
            foreach (var inventory in GetComponentsInChildren<AbsInventory>(true))
            {
                inventory.OnAddItem -= Inventory_OnAddItem;
                inventory.OnRemoveItem -= Inventory_OnRemoveItem;
            }
        }

        private void Inventory_OnAddItem(AbsItem item)
        {
            item.WorldRoot.SetParent(target);

            item.WorldRoot.transform.position = Vector3.zero;
            item.WorldRoot.transform.localRotation = Quaternion.identity;

            item.WorldRoot.gameObject.SetActive(true);
        }

        private void Inventory_OnRemoveItem(AbsItem item)
        {
            item.WorldRoot.gameObject.SetActive(false);
            item.WorldRoot.SetParent(item.transform);
        }
    }
}
