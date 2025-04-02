using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    /// <summary>
    /// Basic enemy stats, does not save, but resets on loading
    /// </summary>
    public class BasicEnemyStats : MonoBehaviour, IEnemyStats, IOnLoadSave
    {
        int _Health = -1;
        public int Health
        {
            get
            {
                if (_Health < 0)
                {
                    _Health = _MaxHealth;
                }
                return _Health;
            }
        }

        [SerializeField]
        int _MaxHealth;
        public int MaxHealth => _MaxHealth;

        [SerializeField]
        int BaseDefence = 0;
        Dictionary<int, int> DefenceModifiers = new Dictionary<int, int>();

        public int TotalArmor => BaseDefence + DefenceModifiers.Values.Sum();

        public bool IsAlive => Health != 0;

        public void Configure(int maxHealth = -1, int baseDefence = -1, int startHealth = -1)
        {
            if (maxHealth >= 0)
            {
                _MaxHealth = maxHealth;
            }

            if (baseDefence > -1)
            {
                BaseDefence = baseDefence;
            }

            if (startHealth > -1)
            {
                _Health = startHealth;
            }
            else if (maxHealth >= 0)
            {
                _Health = maxHealth;
            }
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"Health: {Health}/{MaxHealth}, Defence {BaseDefence} + {DefenceModifiers.Values.Sum()}");
        }

        private void Awake()
        {
            if (_Health < 0)
            {
                _Health = _MaxHealth;
            }
        }

        /// <summary>
        /// Heal enemy
        /// </summary>
        /// <param name="amount">Max that can be healed</param>
        /// <param name="healed">Amount that was healed</param>
        /// <returns>If anything actually was healed</returns>
        public bool Heal(int amount, out int healed)
        {
            healed = Mathf.Min(0, amount, MaxHealth - Health);

            if (healed == 0) return false;

            _Health += healed;
            return true;
        }

        /// <summary>
        /// Hurt enemy
        /// </summary>
        /// <param name="amount">Amount base damage</param>
        /// <param name="hurt">How much enemy was hurt (after defence)</param>
        /// <returns>If enemy was hurt at all</returns>
        public bool Hurt(int amount, out int hurt)
        {
            hurt = Mathf.Clamp(amount - TotalArmor, 0, Health);
            if (hurt == 0) return false;

            _Health -= hurt;
            return true;
        }

        public void Kill()
        {
            _Health = 0;
        }

        /// <summary>
        /// Add a modifier to the enemy's defence
        /// </summary>
        /// <param name="amount">Positive numbers increases defence, negative lowers them</param>
        /// <returns>Id for removing the modifier (see `RemoveArmorModifier`)</returns>
        public int ModifyArmor(int amount)
        {
            var id = DefenceModifiers.Keys.AsEnumerable().FirstUnusedValue();
            DefenceModifiers[id] = amount;
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

                DefenceModifiers.Remove(modifierId);
            }
        }

        public void ResetArmorModifiers()
        {
            DefenceModifiers.Clear();
        }

        #region Load/Save
        public int OnLoadPriority => 750;

        public void OnLoad<T>(T save) where T : new()
        {
            _Health = MaxHealth;
            DefenceModifiers.Clear();
        }
        #endregion
    }
}
