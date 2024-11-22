using LMCore.EntitySM.Trait;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.EntitySM.State
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

    /// <summary>
    /// A criteria for the state of the trait being withing the tolerance
    /// distance from the trait value
    /// </summary>
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

    /// <summary>
    /// A criteria where the current state of a trate is less than
    /// or of the other sign than the trait value
    /// </summary>
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

    /// <summary>
    /// A criteria where the current state of a trate is further from 0
    /// than the trait value is
    /// </summary>
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

    [System.Serializable]
    public class Transition 
    {
        [SerializeField]
        StateType _Target;
        public StateType Target => _Target;

        [SerializeField]
        List<ITransitionCriteria> criteria = new List<ITransitionCriteria>();

        public bool Permissable(Personality personality, out float weight)
        {
            float totalWeight = 0f;
            int n = 0;
            var passing = criteria.All(c => {
                if (c.Permissable(personality, out float criteriaWeight))
                {
                    n++;
                    totalWeight += criteriaWeight;
                    return true;
                }
                return false;
            });

            if (passing)
            {
                weight = totalWeight / n;
                return true;
            } else
            {
                weight = 0f;
                return false;
            }
        }
    }
}
