using System;
using UnityEngine;

namespace LMCore.Inventory
{
    [System.Serializable]
    public class ItemInfo
    {
        public string ItemId;
        public string OriginId;

        public override bool Equals(object obj)
        {
            if (obj is AbsItem)
            {
                var item = (AbsItem)obj;
                return ItemId == item.Id && OriginId == item.Origin;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ItemId, OriginId);
        }
    }

    public abstract class AbsItem : MonoBehaviour
    {
        public abstract string Id { get; }
        public abstract string Origin { get; }

        public string FullId => $"{Origin}-{Id}";

        public abstract bool Stackable { get; }
        public abstract int StackSizeLimit { get; }

        public abstract RectTransform UIRoot { get; }

        public abstract Transform WorldRoot { get; }
    }
}
