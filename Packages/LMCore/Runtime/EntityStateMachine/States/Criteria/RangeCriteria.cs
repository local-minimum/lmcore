using LMCore.EntitySM.Trait;
using UnityEngine;

namespace LMCore.EntitySM.State.Critera
{
    /// <summary>
    /// A criteria for the state of the trait being withing the tolerance
    /// distance from the trait value
    /// </summary>
    [System.Serializable]
    public struct RangeCriteria : ITransitionCriteria
    {
        public TraitType Trait;

        [Range(0f, 1f)]
        public float Tolerance;

        [Range(0f, 1f)]
        public float Noise;

        public bool Permissable(Personality personality, out float weight)
        {
            var state = personality.GetState(Trait);
            var trait = personality.GetTrait(Trait);

            weight = Mathf.Abs(state - trait);
            var diff = weight + Random.Range(-0.5f, 0.5f) * Noise;

            if (diff > Tolerance)
            {
                weight = 0;
                return false;
            }

            return true;
        }
    }
}
