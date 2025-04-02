using UnityEngine;

namespace LMCore.UI
{
    /// <summary>
    /// There's no need in using this other than to show Arc on selected
    /// when selecting the target too.
    /// 
    /// Note: If you use it, don't have it as a child to the target
    /// </summary>
    public class ArcPullerUISource : MonoBehaviour
    {
        [SerializeField]
        BezierUIPuller puller;

        private void OnDrawGizmosSelected()
        {
            if (puller != null)
            {
                puller.OnDrawGizmosSelected();
            }
        }
    }
}
