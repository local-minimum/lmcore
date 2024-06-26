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
        public static T[] FindObjectsByInterface<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) =>
            MonoBehaviour
                .FindObjectsByType<MonoBehaviour>(findObjectsInactive, sortMode)
                .Where(c => typeof(T).IsAssignableFrom(c.GetType()) && typeof(IConvertible).IsAssignableFrom(c.GetType()))
                .Select(c => (T) Convert.ChangeType(c, typeof(T)))
                .ToArray();

        public static T[] FindObjectsByInterface<T>(FindObjectsSortMode sortMode) =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, sortMode);

        public static T[] FindObjectsByInterface<T>() =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        public static T FindObjectByInterface<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) =>
            FindObjectsByInterface<T>(findObjectsInactive, sortMode).First();

        public static T FindObjectByInterface<T>(FindObjectsSortMode sortMode) =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, sortMode).First();

        public static T FindObjectByInterface<T>() =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First();
    }    
}