using LMCore.EntitySM.Trait;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.EntitySM
{
    public class Personality : MonoBehaviour
    {
        [SerializeField]
        private AgressivityTrait _Agressivity;
        [SerializeField]
        private ActivityTrait _Activity;
        [SerializeField]
        private ExplorativityTrait _Explorativity;
        [SerializeField]
        private SociabiliyTrait _Sociability;

        public float GetTrait(TraitType type)
        {
            switch (type)
            {
                case TraitType.Agressivity:
                    return _Agressivity.Value;
                case TraitType.Activity:
                    return _Activity.Value;
                case TraitType.Explorativity:
                    return _Explorativity.Value;
                case TraitType.Sociability:
                    return _Sociability.Value;
                default:
                    throw new NotImplementedException($"{type} not a recognized trait");

            }
        }

        Dictionary<TraitType, float> TraitStates = new Dictionary<TraitType, float>();

        /// <summary>
        /// Get the current state value of the trait
        /// </summary>
        public float GetState(TraitType type) =>
            TraitStates.ContainsKey(type) ? TraitStates[type] : GetTrait(type);

        /// <summary>
        /// Adjust the state of the trait. The value will be scaled depending
        /// on if it's moving towards or away from the trait value
        /// </summary>
        /// <returns>The state value</returns>
        public float AdjustState(TraitType type, float amount)
        {
            var currentValue = GetState(type);
            var traitValue = GetTrait(type);
            var value = currentValue + amount;
            if (Mathf.Sign(traitValue - currentValue) == Mathf.Sign(amount))
            {
                // Adjustment makes us aproach trait value
                var scale = Mathf.Abs(traitValue) + 1;
                value = currentValue + (scale * amount);
            }
            else
            {
                // Adjustment makes us move away from adjustment
                var scale = 1 - Mathf.Abs(traitValue);
                value = currentValue + (scale * amount);
            }

            value = Mathf.Clamp(value, -1, 1);
            TraitStates[type] = value;

            return value;
        }

        public override string ToString()
        {
            return $"{_Agressivity}\t{GetState(TraitType.Agressivity)}\n" +
                $"{_Activity}\t{GetState(TraitType.Activity)}\n" +
                $"{_Explorativity}\t{GetState(TraitType.Explorativity)}\n" +
                $"{_Sociability}\t{GetState(TraitType.Sociability)}";
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(this);
        }
    }
}
