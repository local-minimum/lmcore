using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.EntitySM.State.Critera;

namespace LMCore.EntitySM.State
{
    [System.Serializable]
    public class Transition 
    {
        [SerializeField]
        StateType _Target;
        public StateType Target => _Target;

        [SerializeField]
        List<OvershootsCriteria> _OvershootsCriteria = new List<OvershootsCriteria>();
        [SerializeField]
        List<UndershootCriteria> _UndershootsCriteria = new List<UndershootCriteria>();
        [SerializeField]
        List<RangeCriteria> _RangeCriteria = new List<RangeCriteria>();
        [SerializeField]
        List<NotOpposingSignCriteria> _NotOpposingSignCriteria = new List<NotOpposingSignCriteria>();
        [SerializeField]
        List<NotOfSignCriteria> _NotOfSignCriteria = new List<NotOfSignCriteria>();

        IEnumerable<ITransitionCriteria> criteria
        {
            get
            {
                for (int i = 0, l = _OvershootsCriteria.Count; i < l; i++)
                {
                    yield return _OvershootsCriteria[i];
                }
                for (int i = 0, l = _UndershootsCriteria.Count; i < l; i++)
                {
                    yield return _UndershootsCriteria[i];
                }
                for (int i = 0, l = _RangeCriteria.Count; i < l; i++)
                {
                    yield return _RangeCriteria[i];
                }
                for (int i = 0, l = _NotOpposingSignCriteria.Count; i < l; i++)
                {
                    yield return _NotOpposingSignCriteria[i];
                }
                for (int i = 0, l = _NotOfSignCriteria.Count; i < l; i++)
                {
                    yield return _NotOfSignCriteria[i];
                }
            }
        }

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
