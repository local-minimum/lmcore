using UnityEngine;
using LMCore.EntitySM.Trait;

namespace LMCore.EntitySM.State.Critera
{
    /// <summary>
    /// The current state may not have the specified sign
    /// </summary>
    [System.Serializable]
    public struct NotOfSignCriteria : ITransitionCriteria
    {
        public TraitType Trait;

        public enum SignType { Positive, Negative }

        public SignType Sign;

        [Range(0f, 1f)]
        public float Noise;

        [Range(0f, 1f)]
        public float PassingWeight;

        public bool Permissable(Personality personality, out float weight)
        {
            var state = personality.GetState(Trait) + Random.Range(-0.5f, 0.5f) * Noise;
            var sign = Mathf.Sign(state);
            if (sign == 0 
                || sign == 1 && Sign == SignType.Positive
                || sign == -1 && Sign == SignType.Negative)
            {
                weight = PassingWeight;
                return true;
            }

            weight = 0f;
            return false;
        }
    }
}
