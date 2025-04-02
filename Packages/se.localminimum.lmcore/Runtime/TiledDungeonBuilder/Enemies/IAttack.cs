namespace LMCore.TiledDungeon.Enemies
{
    public interface IAttack
    {
        /// <summary>
        /// The configuration of the entity and its target is such
        /// that the attack would reach and the attack is available
        /// in general
        /// </summary>
        public bool Ready { get; }

        /// <summary>
        /// The attack is currently being performed
        /// </summary>
        public bool Active { get; }

        /// <summary>
        /// If the attack is active and will need to run its course, or if
        /// it is still possible to abort the attack
        /// </summary>
        public bool Committed { get; }

        /// <summary>
        /// Initiate the attack sequence
        /// </summary>
        public void Attack();

        /// <summary>
        /// Abort the attack, should leave the attack as not active
        /// </summary>
        public void Abort();
    }
}
