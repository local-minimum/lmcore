namespace LMCore.EntitySM.State.Critera
{
    public interface ITransitionCriteria
    {
        /// <summary>
        /// If the current state of the personality satisfies the criteria
        /// </summary>
        /// <param name="personality"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public bool Permissable(Personality personality, out float weight);
    }
}
