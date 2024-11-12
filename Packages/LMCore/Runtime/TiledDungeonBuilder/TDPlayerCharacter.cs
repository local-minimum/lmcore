using LMCore.Inventory;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDPlayerCharacter : MonoBehaviour
    {
        [SerializeField]
        string _name = "Alex";
        public string Name => _name;

        [SerializeField]
        int defaultInventorySize = 10;

        public string CharacterId;
        public AbsInventory MainInventory;

        public int MaxHealth { get; private set; } = 100;
        public int Health { get; private set; } = 90;

        public int HealableAmount => MaxHealth - Health;
        public bool FullHealth => Health == MaxHealth;

        public void Heal(int amount)
        {
            Health = Mathf.Clamp(Health + amount, 0, MaxHealth);
        }

        private void Awake()
        {
            if (MainInventory != null) { 
                MainInventory.Configure($"{CharacterId}-Main", null, defaultInventorySize);
            }
        }

        [ContextMenu("Info")]
        public void Info() => Debug.Log($"{Name} {Health}/{MaxHealth}HP");
    }
}
