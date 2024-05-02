using System;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class UnityExtensions
    {
        /// <summary>
        /// Destroy all children of parent
        /// </summary>
        /// <param name="destroy">Destruction function</param>
        public static void DestroyAllChildren(this Transform parent, Action<GameObject> destroy)
        {
            while (parent.childCount > 0)
            {
                Transform child = parent.GetChild(0);
                child.SetParent(null);
                destroy.Invoke(child.gameObject);
            }
        }

        private static void SetAllChildrenVisibility(this Transform parent, bool visible)
        {
            for (int i = 0, n = parent.childCount; i < n; ++i)
            {
                parent.GetChild(i).gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Sets all children as not active
        /// </summary>
        public static void HideAllChildren(this Transform parent) => parent.SetAllChildrenVisibility(false);

        /// <summary>
        /// Sets all children as active
        /// </summary>
        public static void ShowAllChildren(this Transform parent) => parent.SetAllChildrenVisibility(true);
    }
}