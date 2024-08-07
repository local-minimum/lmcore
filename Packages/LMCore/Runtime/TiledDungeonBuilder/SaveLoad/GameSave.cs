using LMCore.Inventory;
using LMCore.IO;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    [System.Serializable]
    public class GameSave 
    {
        public GameEnvironment environment;
        public List<ItemOrigin> disposedItems = new List<ItemOrigin>();

        public GameSave() { 
            environment = new GameEnvironment();
        }
    }
}
