using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.Inventory;
using System.Collections.Generic;
using System.Linq;
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

        [SerializeField]
        AbsInventory _BackpackInventory;
        public AbsInventory BackpackInventory => _BackpackInventory;

        [SerializeField]
        AbsInventory _Equipment;
        public AbsInventory Equipment => _Equipment;

        [SerializeField]
        int StartMaxHealth = 20;
        [SerializeField]
        int StartHealth = 18;

        #region Max Health
        Dictionary<int, int> MaxHealthModifiers = new Dictionary<int, int>();
        public int _MaxHealth = 0;
        /// <summary>
        /// Current max health including temporary modifiers (e.g. from equipment).
        ///
        /// Validates and clamps health into [0 - MaxHealth]
        /// </summary>
        public int MaxHealth
        {
            get => _MaxHealth + MaxHealthModifiers.Values.Sum();
            private set
            {
                _MaxHealth = value;
                Health = Mathf.Clamp(Health, 0, MaxHealth);
            }
        }
        /// <summary>
        /// Modifies the base max health excluding modifiers.
        /// 
        /// Doesn't validate that health is within [0 - MaxHealth (with modifiers)]
        /// </summary>
        public int BaseMaxHealth
        {
            get => _MaxHealth;
            set => _MaxHealth = value;
        }

        /// <summary>
        /// Add temporary max health
        /// </summary>
        /// <param name="amount">Amount max health adjustmen</param>
        /// <returns>Identifier used to remove added health</returns>
        public int ModifyMaxHealth(int amount)
        {
            var id = MaxHealthModifiers.Keys.AsEnumerable().FirstUnusedValue();
            MaxHealthModifiers[id] = amount;
            Debug.Log(PrefixLogMessage($"Added {amount} max health (id: {id})"));
            return id;
        }

        /// <summary>
        /// Remove max health modifier
        /// </summary>
        /// <param name="modifierId">Identifier associated with adjustment</param>
        public void RemoveMaxHealthModifier(int modifierId)
        {
            if (MaxHealthModifiers.ContainsKey(modifierId))
            {
                var def = MaxHealthModifiers[modifierId];
                MaxHealthModifiers.Remove(modifierId);
                Debug.Log(PrefixLogMessage($"Lost {def} max health modifier (id: {modifierId})"));
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage($"Tried to remove max health {modifierId}, " +
                    $"but not know: {string.Join(", ", MaxHealthModifiers.Keys)}"));
            }
        }
        #endregion

        public int Health { get; private set; } = 0;

        /// <summary>
        /// Sets current health and base max health (without modifiers)
        ///
        /// Note that this allows health to surpass max-health
        /// </summary>
        /// <param name="health"></param>
        /// <param name="maxHealth"></param>
        public void LoadHealth(int health, int maxHealth)
        {
            maxHealth = Mathf.Max(maxHealth, 0);
            health = Mathf.Max(health, 0);

            BaseMaxHealth = maxHealth;
            Health = health;
            Debug.Log(PrefixLogMessage($"Synched health from load {health}/{maxHealth} (base max health) HP"));
        }

        public int XP { get; set; } = 0;
        public int Level { get; set; } = 1;

        public int HealableAmount => MaxHealth - Health;
        public bool FullHealth => Health == MaxHealth;
        public bool Alive => Health > 0;

        #region Defence
        [SerializeField]
        int _baseDefence = 0;
        /// <summary>
        /// Defence value without temporary modifiers
        /// </summary>
        public int BaseDefence
        {
            get => _baseDefence;
            set => _baseDefence = value;
        }

        Dictionary<int, int> DefenceModifiers = new Dictionary<int, int>();

        public int TotalArmor => _baseDefence + DefenceModifiers.Values.Sum();

        protected string PrefixLogMessage(string message) =>
            $"{Name} {Health}/{MaxHealth}({BaseMaxHealth})HP {TotalArmor}DEF: {message}";

        /// <summary>
        /// Add a modifier to the character's defence
        /// </summary>
        /// <param name="amount">Positive numbers increases defence, negative lowers them</param>
        /// <returns>Id for removing the modifier (see `RemoveArmorModifier`)</returns>
        public int ModifyArmor(int amount)
        {
            var id = DefenceModifiers.Keys.AsEnumerable().FirstUnusedValue();
            DefenceModifiers[id] = amount;
            Debug.Log(PrefixLogMessage($"Added {amount} defence (id: {id})"));
            return id;
        }

        /// <summary>
        /// Removes a modifier that was added by `ModifyArmor`
        /// </summary>
        /// <param name="modifierId">The id for the modifier</param>
        public void RemoveArmorModifier(int modifierId)
        {
            if (DefenceModifiers.ContainsKey(modifierId))
            {
                var def = DefenceModifiers[modifierId];
                if (!DefenceModifiers.Remove(modifierId))
                {
                    Debug.LogError(PrefixLogMessage($"Could not remove defence modifier {modifierId}"));
                    return;
                }
                Debug.Log(PrefixLogMessage($"Lost {def} defence modifier (id: {modifierId})"));
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage($"Tried to remove defence {modifierId}, " +
                    $"but not know: {string.Join(", ", DefenceModifiers.Keys)}"));
            }
        }
        #endregion

        /// <summary>
        /// Heals a certain amount, but probably should not be used directly.
        /// 
        /// Prefer using the TDDamageSystem.
        /// </summary>
        public void Heal(int amount, out int heal)
        {
            heal = Mathf.Clamp(amount, 0, HealableAmount);
            Health += heal;
            Debug.Log(PrefixLogMessage($"Recieved {heal} health"));
        }

        /// <summary>
        /// Hurts a certain amount, but probably should not be used directly.
        /// 
        /// Prefer using the TDDamageSystem.
        /// </summary>
        public void Hurt(int amount, out int hurt)
        {
            hurt = Mathf.Clamp(amount - TotalArmor, 0, Health);
            Health = Mathf.Clamp(Health - hurt, 0, MaxHealth);
            Debug.Log(PrefixLogMessage($"Took {hurt} damage"));
        }

        private void Awake()
        {
            if (MaxHealth == 0 && Health == 0)
            {
                Debug.Log(PrefixLogMessage("Setting start health"));
                BaseMaxHealth = StartMaxHealth;
                Health = StartHealth;
            }

            if (BackpackInventory != null)
            {
                BackpackInventory.Configure($"{CharacterId}-Backpack", null, defaultInventorySize);
            }

            if (Equipment != null)
            {
                Equipment.Configure($"{CharacterId}-Equipment");
            }
        }

        [ContextMenu("Info")]
        public void Info() => Debug.Log(PrefixLogMessage("Nothing more to say"));

        [DevConsole.Command(new string[] { "character" }, "Adjust health and XP")]
        private static void DCCharacterContext(string payload)
        {
            DevConsole.DevConsole.AddContext("character");

            if (!string.IsNullOrEmpty(payload?.Trim()))
            {
                DevConsole.DevConsole.ProcessInput(payload);
                DevConsole.DevConsole.RemoveOuterContext("character");
            }
            else
            {
                DevConsole.DevConsole.ListCommands();
            }
        }

        private static GridEntity DCGetPlayer()
        {
            var dungeon = GameObject.FindFirstObjectByType<TiledDungeon>();
            if (dungeon == null)
            {
                DevConsole.DevConsole.Error($"Not currently in a dungeon");
                return null;
            }

            var player = dungeon.Player;
            if (player == null)
            {
                DevConsole.DevConsole.Error($"No player in the dungeon");
                return null;
            }

            return player;
        }

        private static TDPlayerCharacter DCGetTarget(string target)
        {
            var player = DCGetPlayer();
            var targetChar = player
                .GetComponentsInChildren<TDPlayerCharacter>()
                .FirstOrDefault(p => string.IsNullOrEmpty(target) || p.CharacterId == target);

            if (targetChar == null)
            {
                DevConsole.DevConsole.Error($"No character '{target}' on {player.Identifier}");
            }

            return targetChar;
        }

        [DevConsole.Command(new string[] { "character", "health" }, "[amount (negative hurt)] [character-id (optional))]")]
        private static void DCCharacterHeal(string payload)
        {
            var amount = DevConsole.DevConsole.SplitCommandAndPayload(payload, out string target);

            if (string.IsNullOrEmpty(amount))
            {
                DevConsole.DevConsole.Log("Usage: [amount (negative hurt)] [character-id (optional))]");
                return;
            }


            if (int.TryParse(amount.Trim(), out int hp))
            {
                var targetChar = DCGetTarget(target);
                if (targetChar == null) return;

                if (hp > 0)
                {
                    var healed = TDDamageSystem.instance.HealCharacter(targetChar, hp);
                    DevConsole.DevConsole.Log($"Healed {targetChar.Name} {healed} HP to {targetChar.Health} HP");
                }
                else
                {
                    int hurt = TDDamageSystem.instance.HurtCharacter(targetChar, Mathf.Abs(hp));
                    DevConsole.DevConsole.Log($"Hurt {targetChar.Name} {hurt} HP to {targetChar.Health} HP");
                }
            }
            else
            {
                DevConsole.DevConsole.Error($"'{amount}' not understood as an int");
            }
        }

        [DevConsole.Command(new string[] { "character", "xp" }, "[amount] [character-id (optional))]")]
        private static void DCCharacterXP(string payload)
        {
            var amount = DevConsole.DevConsole.SplitCommandAndPayload(payload, out string target);

            if (string.IsNullOrEmpty(amount))
            {
                DevConsole.DevConsole.Log("Usage: [amount] [character-id (optional))]");
                return;
            }


            if (int.TryParse(amount.Trim(), out int xp))
            {
                var targetChar = DCGetTarget(target);
                if (targetChar == null) return;

                if (xp > 0)
                {
                    var xpSystem = targetChar.GetComponentInParent<TDExperienceSystem>();
                    if (xpSystem == null)
                    {
                        DevConsole.DevConsole.Error($"Could not locate an XP-system associated with {targetChar.Name}");
                        targetChar.XP += xp;
                    }
                    else
                    {
                        xpSystem.GainXP(targetChar, xp);
                    }
                    DevConsole.DevConsole.Log($"{targetChar.Name} gained {xp} XP, now at {targetChar.XP} XP (level {targetChar.Level})");
                }
                else
                {
                    DevConsole.DevConsole.Error($"XP gain must be positive value (was {xp})");
                }
            }
            else
            {
                DevConsole.DevConsole.Error($"'{amount}' not understood as an int");
            }
        }

        [DevConsole.Command(new string[] { "character", "teleport" }, "Teleports character to absolute position")]
        private static void DCCharacterTeleport(string payload)
        {
            payload = payload.Trim();
            if (string.IsNullOrEmpty(payload))
            {
                DevConsole.DevConsole.Log("Give position in format: (x, y, z)");
                return;
            }

            if (DevConsole.DevConsole.ParseVector3Int(payload, out var coordinates))
            {
                var targetChar = DCGetPlayer();
                if (targetChar == null) return;

                targetChar.Coordinates = coordinates;
                targetChar.Sync();
            }
            else
            {
                DevConsole.DevConsole.Error($"Could not parse '{payload}' as a 3D int vector");
            }
        }

        [DevConsole.Command(new string[] { "character", "relative-teleport" }, "Teleports character relative to its current position")]
        private static void DCCharacterRelativeTeleport(string payload)
        {
            payload = payload.Trim();
            if (string.IsNullOrEmpty(payload))
            {
                DevConsole.DevConsole.Log("Give position in format: (right, up, forward)\nOr use words like 'forward', 'left', 'down'");
                return;
            }

            if (DevConsole.DevConsole.ParseVector3Int(payload, out var offset))
            {
                var targetChar = DCGetPlayer();
                if (targetChar == null) return;

                targetChar.Coordinates += offset.RelativeFrame(
                   targetChar.LookDirection.AsLookVector3D(),
                   targetChar.Down.AsLookVector3D());

                targetChar.Sync();
            }
            else
            {
                DevConsole.DevConsole.Error($"Could not parse '{payload}' as a 3D int vector");
            }
        }
    }
}
