using UnityEngine;

namespace LMCore.Extensions
{
    public static class AnimatorExtensions
    {
        public static bool IsActiveAnimation(this Animator animator, int animLayer, string animationName) =>
            animator.GetCurrentAnimatorStateInfo(animLayer).IsName(animationName);

        public static bool IsAnimating(this Animator animator, int animLayer) =>
            animator.GetCurrentAnimatorStateInfo(animLayer).normalizedTime < 1.0f;

        public static bool IsAnimating(this Animator animator, int animLayer, string animationName) =>
            animator.GetCurrentAnimatorStateInfo(animLayer).IsName(animationName)
            && animator.GetCurrentAnimatorStateInfo(animLayer).normalizedTime < 1.0f;

        #region AnimationCurve
        /// <summary>
        /// Returns the time of the last key in the curve
        /// </summary>
        public static float Duration(this AnimationCurve curve) =>
            curve.length == 0 ? 0 : curve.keys[curve.keys.Length - 1].time;
        #endregion
    }
}
