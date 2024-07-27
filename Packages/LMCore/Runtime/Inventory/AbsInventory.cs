using UnityEngine;

namespace LMCore.Inventory
{
    [System.Serializable]
    public abstract class AbsInventory : MonoBehaviour
    {
        public abstract bool HasItem(string item);
        public abstract bool HasItem(string item, string identifier);

        public abstract bool Consume(string item);
        public abstract bool Consume(string item, string identifier);
    }
}
