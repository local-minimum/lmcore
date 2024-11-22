using System;

namespace LMCore.EntitySM.Trait
{
    [Serializable]
    public class SociabiliyTrait : AbsTrait
    {
        public override TraitType Type => TraitType.Sociability;
        public override string LowValueName => "Loner";
        public override string HighValueName => "Social";
    }
}
