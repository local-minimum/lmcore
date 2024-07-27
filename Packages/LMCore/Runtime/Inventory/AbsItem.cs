using UnityEngine;

namespace LMCore.Inventory
{
    public abstract class AbsItem : MonoBehaviour
    {
        public abstract string Id { get; }
        public abstract string Origin { get; }

        public bool Stackable { get; }
        public int StackSizeLimit { get; }

        public abstract RectTransform UIRoot { get; }

        public abstract Transform WorldRoot { get; }
    }
}
