using UnityEngine;

namespace LMCore.Crawler
{
    [System.Serializable]
    public struct EntityAbilities 
    {
        public float maxScaleHeight;
        public float maxHorizontalJump;
        [Tooltip("Smaller than this is just walking over")]
        public float minHorizontalJump;
    }
}
