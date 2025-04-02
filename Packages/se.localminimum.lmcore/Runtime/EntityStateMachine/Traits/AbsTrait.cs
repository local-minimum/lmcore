using System;
using System.Text;
using UnityEngine;

namespace LMCore.EntitySM.Trait
{
    [Serializable]
    public abstract class AbsTrait
    {
        public abstract TraitType Type { get; }
        public abstract string LowValueName { get; }
        public abstract string HighValueName { get; }

        [Range(-1f, 1f)]
        public float Value;

        public override string ToString() =>
            $"{LowValueName}\t{ValueToProgressString(Value)} {HighValueName}\t({Value})";

        private static string ValueToProgressString(float value, int size = 5)
        {
            var builder = new StringBuilder(new string('-', 1 + (size * 2)));
            var step = size + Mathf.RoundToInt(value * size);
            builder[step] = 'x';
            return $"<{builder}>";
        }
    }
}
