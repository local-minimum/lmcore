using UnityEngine;

namespace LMCore.EntitySM.State.Critera
{
    public abstract class AbsCustomPassingCriteria : MonoBehaviour
    {
        public abstract bool Passing { get; }
    }

    /// <summary>
    /// A criteria that is goverened by the state of a behaviour elsewhere
    /// </summary>
    [System.Serializable]
    public class CustomCriteria : ITransitionCriteria
    {
        [SerializeField]
        float Weight = 1f;

        [SerializeField]
        AbsCustomPassingCriteria criteria;

        public bool Permissable(Personality personality, out float weight)
        {
            weight = Weight;
            return criteria.Passing;
        }
    }
}
