using LMCore.Inventory;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDPlayerCharacter : MonoBehaviour
    {
        [SerializeField]
        int defaultInventorySize = 10;

        public string CharacterId;
        public AbsInventory MainInventory;

        private void Awake()
        {
            if (MainInventory != null) { 
                MainInventory.Configure($"{CharacterId}-Main", null, defaultInventorySize);
            }
        }
    }
}
