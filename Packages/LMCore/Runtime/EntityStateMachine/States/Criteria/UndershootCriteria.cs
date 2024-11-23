using LMCore.EntitySM.Trait;
using UnityEngine;

namespace LMCore.EntitySM.State.Critera
{
    /// <summary>
    /// A criteria where the current state of a trate is less than
    /// or of the other sign than the trait value
    /// </summary>
    [System.Serializable]
    public struct UndershootCriteria : ITransitionCriteria
    {
        public TraitType Trait;

        [Range(0f, 1f)]
        public float Amount;

        [Range(0f, 1f)]
        public float Noise;

        public bool Permissable(Personality personality, out float weight)
        {
            var state = personality.GetState(Trait);
            var trait = personality.GetTrait(Trait);

            if (trait == 0)
            {
                Debug.LogError($"Undershoot trait for {personality}: {Trait} can nerver be true, because it is zero.");
                weight = 0f;
                return false;
            }

            // If trait is positive and larger than state both factors are positive
            // If trait is negative and larger than state both factors are negative
            weight = (trait - state) * Mathf.Sign(trait);
            var diff = weight + Random.Range(-0.5f, 0.5f) * Noise;
            if (weight < 0 || diff < Amount)
            {
                weight = 0f;
                return false;
            }

            // Since the maximum undershoot is much larger than the overshoot we half the weight
            weight /= 2f;
            return true;
        }
    }
}
