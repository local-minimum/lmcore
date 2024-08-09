using LMCore.Inventory;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    [System.Serializable]
    public class InventorySave<T>
    {
        public List<T> items;
        public string fullId;

        public InventorySave(string fullId, IEnumerable<T> items) {
            this.fullId = fullId;
            this.items = items.ToList();
        }
    }

    [System.Serializable]
    public class ContainerSave<T>
    {
        public TDContainer.ContainerPhase phase;
        public List<InventorySave<T>> inventories;

        public ContainerSave(
            TDContainer.ContainerPhase phase, 
            IEnumerable<InventorySave<T>> inventories)
        {
            this.inventories = inventories.ToList();
            this.phase = phase;
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
