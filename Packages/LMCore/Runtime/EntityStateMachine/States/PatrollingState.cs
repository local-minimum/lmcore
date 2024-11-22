using LMCore.EntitySM.State;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.EntitySM
{
    public class PatrollingState : AbsState
    {
        public override StateType State => StateType.Patrolling;

        public override bool CheckTransition(Personality personality, out StateType newStateType)
        {
            throw new System.NotImplementedException();
        }

        protected override void _Enter()
        {
        }

        protected override void _Exit()
        {
        }
    }
}
