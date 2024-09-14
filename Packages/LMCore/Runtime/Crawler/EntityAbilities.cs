using UnityEngine;

namespace LMCore.Crawler
{
    [System.Serializable]
    public struct EntityAbilities 
    {
        [Range(0f, 1f)]
        public float turnDurationScaleFactor;

        [Range(0f, 1f), Tooltip("Factor of distance to the midpoint where movement should stop")]
        public float refusedMidpointMaxInterpolation;

        [Range(0f, 1f), Tooltip("Factor of distance to the midpoint where movement should stop when reversing")]
        public float refusedMidpointReversingMaxInterpolation;

        [Tooltip("Inverse direction of current down. Smaller than this is ignored / walked over")]
        public float minScaleHeight;
        [Tooltip("Inverse direction of current down. Larger than this is refused")]
        public float maxScaleHeight;

        [Tooltip("Smaller than this is ignored / walked over")]
        public float minForwardJump;
        [Tooltip("Larger than this is refused")]
        public float maxForwardJump;

        [Tooltip("When jumping is in the inverse direction down")]
        public float jumpHeight;


        [Tooltip("Should probably be 1, 2, 4, 6 or 8")]
        public int walkingStepsPerTransition;

        [Tooltip("Should probably be 1, 2, 4, 6 or 8")]
        public int climbingStepsPerTransition;

        [Tooltip("Should probably be 1, 2, 4, 6 or 8")]
        public int stairsStepsPerTransition;
    }
}
