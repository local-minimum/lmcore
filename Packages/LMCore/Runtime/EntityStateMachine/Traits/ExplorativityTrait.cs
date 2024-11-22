using System;

namespace LMCore.EntitySM.Trait
{
    [Serializable]
    public class ExplorativityTrait : AbsTrait
    {
        public override TraitType Type => TraitType.Explorativity;
        public override string LowValueName => "Dweller";
        public override string HighValueName => "Globetrotter";
    }
}
