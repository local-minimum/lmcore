using LMCore.EntitySM.Trait;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.EntitySM.State.Critera
{
    /// <summary>
    /// State is amount less than trait
    /// </summary>
    [System.Serializable]
    public class LessCriteria : ITransitionCriteria
    {
        public TraitType Trait;

        [Range(0f, 1f)]
        public float Noise;

        [Range(0f, 1f)]
        public float Amount;

        [Range(0f, 1f)]
        public float MinWeight = 0.5f;

        bool ITransitionCriteria.Permissable(Personality personality, out float weight)
        {
            var state = personality.GetState(Trait);
            var trait = personality.GetTrait(Trait);

            if (state > trait || state + Random.Range(-0.5f, 0.5f) * Noise > trait - Amount)
            {
                weight = 0f;
                return false;
            }

            weight = Mathf.Max(Mathf.Abs(trait - state), MinWeight);
            return true;
        }
    }
}
