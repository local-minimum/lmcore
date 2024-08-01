using LMCore.AbstractClasses;

namespace LMCore.Inventory
{
    public abstract class AbsItemFactory : Singleton<AbsInventory> 
    {
        public abstract bool Create(string itemId, string originId, out AbsItem item);
    }
}
