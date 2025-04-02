using LMCore.Inventory;
using UnityEngine;

namespace LMCore.Crawler
{
    public class FloorLootNode : MonoBehaviour
    {
        Anchor _anchor;
        public Anchor anchor
        {
            get
            {
                if (_anchor == null)
                {
                    _anchor = GetComponentInParent<Anchor>();
                }
                return _anchor;
            }
        }

        AbsInventory _inventory;
        public AbsInventory inventory
        {
            get
            {
                if (_inventory == null)
                {
                    _inventory = GetComponent<AbsInventory>();
                }
                return _inventory;
            }
        }

        LevelFloorContainer _container;
        LevelFloorContainer container
        {
            get
            {
                if (_container == null)
                {
                    _container = GetComponentInParent<LevelFloorContainer>(true);
                }

                return _container;
            }
        }

        WorldInventoryDisplay _display;
        WorldInventoryDisplay Display
        {
            get
            {
                if (_display == null)
                {
                    _display = GetComponent<WorldInventoryDisplay>();
                }
                return _display;
            }
        }

        const int DefaultCapacity = 3;

        public void Sync()
        {
            var inv = inventory;
            var anchor = this.anchor;
            if (inv == null || anchor == null)
            {
                return;
            }

            var c = anchor.Node.Coordinates;
            int capacity = Mathf.Max(DefaultCapacity, inv.Capacity);
            if (Display != null)
            {
                capacity = Mathf.Max(capacity, Display.LocationCount);
            }

            inv.Configure($"{c.x}:{c.y}:{c.z}", null, capacity);
        }
    }
}
