using System;

namespace LMCore.EntitySM.Trait
{
    [Serializable]
    public class ActivityTrait : AbsTrait
    {
        public override TraitType Type => TraitType.Activity;
        public override string LowValueName => "Lazy";
        public override string HighValueName => "Sporty";
    }
}
