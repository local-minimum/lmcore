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

        public int Health { get; private set; } = 100;
        public bool FullHealth => Health == 100;

        private void Awake()
        {
            if (MainInventory != null) { 
                MainInventory.Configure($"{CharacterId}-Main", null, defaultInventorySize);
            }
        }
    }
}
