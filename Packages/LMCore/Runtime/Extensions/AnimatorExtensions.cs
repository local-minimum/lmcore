using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class AnimatorExtensions {
        public static bool IsActiveAnimation(this Animator animator, int animLayer, string animationName) =>
            animator.GetCurrentAnimatorStateInfo(animLayer).IsName(animationName);

        public static bool IsAnimating(this Animator animator, int animLayer) =>
            animator.GetCurrentAnimatorStateInfo(animLayer).normalizedTime < 1.0f;

        public static bool IsAnimating(this Animator animator, int animLayer, string animationName) =>
            animator.GetCurrentAnimatorStateInfo(animLayer).IsName(animationName)
            && animator.GetCurrentAnimatorStateInfo(animLayer).normalizedTime < 1.0f;
    }
}
