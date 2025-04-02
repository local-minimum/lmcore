using LMCore.Inventory;

namespace LMCore.TiledDungeon
{
    [System.Serializable]
    public class StackedItemInfo : ItemInfo
    {
        public int StackIndex;

        public StackedItemInfo(AbsItem item, int stackId)
        {
            StackIndex = stackId;
            ItemId = item.Id;
            OriginId = item.Origin;
        }

        public StackedItemInfo(string id, string origin, int stackId)
        {
            StackIndex = stackId;
            OriginId = origin;
            ItemId = id;
        }
    }
}
