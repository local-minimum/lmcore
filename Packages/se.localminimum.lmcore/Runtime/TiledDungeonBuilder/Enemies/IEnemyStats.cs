namespace LMCore.TiledDungeon.Enemies
{
    public interface IEnemyStats
    {
        public int Health { get; }
        public int MaxHealth { get; }

        public int TotalArmor { get; }

        public bool IsAlive { get; }

        public bool Hurt(int amount, out int hurt);

        /// <summary>
        /// Typically only used when loading saves and such forcing death state
        /// </summary>
        public void Kill();
        public bool Heal(int amount, out int healed);

        public int ModifyArmor(int amount);
        public void RemoveArmorModifier(int modifierId);
        public void ResetArmorModifiers();

        public void Configure(int maxHealth = -1, int baseDefence = -1, int startHealth = -1);
    }
}
