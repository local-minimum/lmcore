using System;
using System.Linq;
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

        public static Canvas GetCanvas(this RectTransform transform) => transform.GetComponentInParent<Canvas>();

        public static Vector2 CalculateSize(this RectTransform transform)
        {
            var anchorDelta = (transform.anchorMax - transform.anchorMin);
            var parentSize = ((RectTransform)transform.parent)?.CalculateSize() ?? transform.GetCanvas().pixelRect.size;
            var size = anchorDelta * parentSize;
            if (size.x == 0) size.x = transform.sizeDelta.x;
            if (size.y == 0) size.y = transform.sizeDelta.y;

            return size;
        }

        // TODO: This doesn't seem to work
        /// <summary>
        /// Find all mono behaviours that implement an interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="findObjectsInactive"></param>
        /// <param name="sortMode"></param>
        /// <returns></returns>
        public static T[] FindObjectsByInterface<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) where T : class =>
            MonoBehaviour
                .FindObjectsByType<MonoBehaviour>(findObjectsInactive, sortMode)
                .Where(c => c is T)
                .Select(c => c as T)
                .ToArray();

        public static T[] FindObjectsByInterface<T>(FindObjectsSortMode sortMode) where T : class =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, sortMode);

        public static T[] FindObjectsByInterface<T>() where T : class =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        public static T FindObjectByInterface<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) where T : class =>
            FindObjectsByInterface<T>(findObjectsInactive, sortMode).First();

        public static T FindObjectByInterface<T>(FindObjectsSortMode sortMode) where T : class =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, sortMode).First();

        public static T FindObjectByInterface<T>() where T: class =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First();
        public static T FindObjectByInterfaceOrDefault<T>() where T : class =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault();
    }    
}