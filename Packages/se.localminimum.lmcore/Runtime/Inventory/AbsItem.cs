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

    [System.Flags]
    public enum ItemType
    {
        Nothing = 0,
        Consumable = 1,
        Equipment = 2,
        Tool = 4,
        Document = 8,
        QuestItem = 16,
    }

    public enum EquipmentType
    {
        None,
        Head,
        Neck,
        Chest,
        Fingers,
        Hands,
        Legs,
        Feet,
        Weapon,
        Container,
        Utility,
    }

    public abstract class AbsItem : MonoBehaviour
    {
        public abstract string Id { get; }
        public abstract string Origin { get; }

        public string FullId => $"{Origin}-{Id}";

        public abstract ItemType Type { get; }
        public abstract EquipmentType Equipment { get; }

        public abstract bool Stackable { get; }
        public abstract int StackSizeLimit { get; }

        public abstract Sprite UISprite { get; }

        public abstract Transform WorldRoot { get; }

        public abstract string Name { get; }
        public abstract string Description { get; }
    }
}
