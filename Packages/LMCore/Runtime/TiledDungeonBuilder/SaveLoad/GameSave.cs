using LMCore.Inventory;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    [System.Serializable]
    public class ContainerSave<T>
    {
        public List<T> inventories;

        public ContainerSave(IEnumerable<T> inventories)
        {
            this.inventories = inventories.ToList();
        }
    }

    [System.Serializable]
    public class LevelSave
    {
        // TODO: Should not be abs item!
        public SerializableDictionary<string, ContainerSave<StackedItemInfo>> TD1DInventories = new ();
    }

    [System.Serializable]
    public class GameSave 
    {
        public GameEnvironment environment;
        public List<ItemOrigin> disposedItems = new List<ItemOrigin>();
        public SerializableDictionary<string, LevelSave> levels = new SerializableDictionary<string, LevelSave>();

        public GameSave() { 
            environment = new GameEnvironment();
            
        }
    }
}
