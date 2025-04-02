using LMCore.EntitySM.State.Critera;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    [RequireComponent(typeof(TDEnemyAttacking))]
    public class AttackingPossibleCriteria : AbsCustomPassingCriteria
    {
        public override bool Passing =>
            GetComponent<TDEnemyAttacking>().attacks.Any(a => a.Ready);
    }
}
