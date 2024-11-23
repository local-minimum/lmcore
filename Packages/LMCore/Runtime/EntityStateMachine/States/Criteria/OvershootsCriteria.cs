using LMCore.EntitySM.Trait;
using UnityEngine;

namespace LMCore.EntitySM.State.Critera
{
    /// <summary>
    /// A criteria where the current state of a trate is further from 0
    /// than the trait value is
    /// </summary>
    [System.Serializable]
    public struct OvershootsCriteria : ITransitionCriteria
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

            if (trait == 0 || Mathf.Sign(state) == Mathf.Sign(trait))
            {
                weight = (Mathf.Abs(state) - Mathf.Abs(trait));
                var diff = weight + Random.Range(-0.5f, 0.5f) * Noise;

                // We can have a negative weigth with larger noise overshoot the amount
                // but we should ensure we never output negative weights
                weight = Mathf.Max(0, weight);

                return diff >= Amount;
            }

            weight = 0;
            return false;
        }
    }
}
