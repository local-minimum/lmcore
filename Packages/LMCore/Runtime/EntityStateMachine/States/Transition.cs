using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.EntitySM.State.Critera;
using System.Text;

namespace LMCore.EntitySM.State
{
    [System.Serializable]
    public class Transition 
    {
        [SerializeField]
        StateType _Target;
        public StateType Target => _Target;

        [SerializeField]
        List<LessCriteria> _LessCriteria = new List<LessCriteria>();
        [SerializeField]
        List<MoreCriteria> _MoreCriteria = new List<MoreCriteria>();
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
        [SerializeField]
        List<CustomCriteria> _CustomCriteria = new List<CustomCriteria>();

        IEnumerable<ITransitionCriteria> criteria
        {
            get
            {
                for (int i = 0, l = _LessCriteria.Count; i < l; i++)
                {
                    yield return _LessCriteria[i];
                }
                for (int i = 0, l = _MoreCriteria.Count; i < l; i++)
                {
                    yield return _MoreCriteria[i];
                }
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
                for (int i = 0, l = _CustomCriteria.Count; i < l; i++)
                {
                    yield return _CustomCriteria[i];
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

        public string ToString(Personality personality)
        {
            var msg = new List<string>();
            foreach (var c in criteria)
            {
                if (c.Permissable(personality, out float w))
                {
                    msg.Add($"<{c} weight: {w}>");
                }
                else
                {
                    msg.Add($"<{c} FAIL>");
                }
            }

            return string.Join(" ", msg);
        }
    }
}
