using LMCore.AbstractClasses;

namespace LMCore.Inventory
{
    public abstract class AbsItemFactory<T> : Singleton<AbsItemFactory<T>, T> where T : AbsItemFactory<T>
    {
        public abstract bool Create(string itemId, string originId, out AbsItem item);
        public abstract void Recycle(AbsItem item);
    }
}
