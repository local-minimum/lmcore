using LMCore.EntitySM.State.Critera;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.EntitySM.State
{
    [System.Serializable]
    public class Transition
    {
        [SerializeField]
        StateType _Target;
        public StateType Target => _Target;

        [SerializeField]
        List<CustomCriteria> _CustomCriteria = new List<CustomCriteria>();

        IEnumerable<ITransitionCriteria> criteria
        {
            get
            {
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
            var passing = criteria.All(c =>
            {
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
            }
            else
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

            return $"[{Target} {string.Join(" ", msg)}]";
        }
    }
}
