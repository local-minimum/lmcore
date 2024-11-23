using LMCore.EntitySM.Trait;
using UnityEngine;

namespace LMCore.EntitySM.State.Critera
{
    /// <summary>
    /// The current state of the trait and the trait value may not have 
    /// opposing signs
    /// </summary>
    [System.Serializable]
    public struct NotOpposingSignCriteria : ITransitionCriteria
    {
        public TraitType Trait;

        [Range(0f, 1f)]
        public float PassingWeight;

        [Range(0f, 1f)]
        public float Noise;

        public bool Permissable(Personality personality, out float weight)
        {
            var state = personality.GetState(Trait) + Random.Range(-0.5f, 0.5f) * Noise;
            var trait = personality.GetTrait(Trait);
            if (trait == 0 || state == 0 || Mathf.Sign(state) == Mathf.Sign(trait))
            {
                weight = PassingWeight;
                return true;
            }

            weight = 0f;
            return false;
        }
    }
}
