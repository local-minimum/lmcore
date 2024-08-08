using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.Inventory
{
    public class RecycleBin : Singleton<RecycleBin> 
    {
        List<AbsItem> recycledItems = new List<AbsItem>();

        /// <summary>
        /// If an inventory when loading no longer has an item it should add it here
        /// </summary>
        /// <param name="item"></param>
        public void Add(AbsItem item)
        {
            recycledItems.Add(item);
        }

        /// <summary>
        /// Getting items when loading for things that wasn't yours originally
        /// </summary>
        /// <param name="id"></param>
        /// <param name="origin"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(string id, string origin, out AbsItem item)
        {
            var candidate = recycledItems.FirstOrDefault(i => i.Id == id && i.Origin == origin);

            if (candidate != null)
            {
                item = candidate;
                recycledItems.Remove(candidate);
                return true;
            }

            item = null;
            return false;
        }
    }
}
