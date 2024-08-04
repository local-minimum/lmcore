using LMCore.AbstractClasses;

namespace LMCore.Inventory
{
    public abstract class AbsItemFactory : Singleton<AbsItemFactory> 
    {
        public abstract bool Create(string itemId, string originId, out AbsItem item);
    }
}
