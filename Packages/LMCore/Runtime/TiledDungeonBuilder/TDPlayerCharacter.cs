using LMCore.Inventory;
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

        protected string PrefixLogMessage(string message) =>
            $"{Name} {Health}/{MaxHealth}HP: {message}";

        public void Heal(int amount)
        {
            Health = Mathf.Clamp(Health + amount, 0, MaxHealth);
            Debug.Log(PrefixLogMessage($"Recieved {amount} health"));
        }

        public void Hurt(int amount)
        {
            Health = Mathf.Clamp(Health - amount, 0, MaxHealth);
            Debug.Log(PrefixLogMessage($"Took {amount} damage"));
        }

        private void Awake()
        {
            if (MainInventory != null) { 
                MainInventory.Configure($"{CharacterId}-Main", null, defaultInventorySize);
            }
        }

        [ContextMenu("Info")]
        public void Info() => Debug.Log(PrefixLogMessage("Nothing more to say"));
    }
}
