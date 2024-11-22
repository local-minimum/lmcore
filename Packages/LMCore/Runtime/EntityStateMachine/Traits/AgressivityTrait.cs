using System;

namespace LMCore.EntitySM.Trait
{
    [Serializable]
    public class AgressivityTrait : AbsTrait
    {
        public override TraitType Type => TraitType.Agressivity;
        public override string LowValueName => "Evasive";
        public override string HighValueName => "Agressive";
    }
}
