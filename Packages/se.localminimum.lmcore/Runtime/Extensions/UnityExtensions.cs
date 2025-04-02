using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LMCore.Extensions
{
    public enum SimplifiedDevice { XBoxController, PSController, SwitchController, OtherController, MouseAndKeyboard };

    public static class UnityExtensions
    {
        #region Children

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

        public static IEnumerable<GameObject> WalkChildrenRecursively(
            this GameObject parent,
            Func<GameObject, bool> filter)
        {
            for (int i = 0, l = parent.transform.childCount; i < l; i++)
            {
                var child = parent.transform.GetChild(i);
                if (!filter(child.gameObject)) continue;

                yield return child.gameObject;

                foreach (var decendant in child.gameObject.WalkChildrenRecursively(filter))
                {
                    yield return decendant;
                }

            }
        }

        #endregion

        #region Parents
        public static IEnumerable<T> WalkParentsRecursively<T>(
            this GameObject origin,
            Func<GameObject, bool> filter,
            Func<GameObject, T> predicate)
        {
            var current = origin.transform.parent.gameObject;

            while (current != null)
            {
                if (filter(current))
                {
                    yield return predicate(current);
                }

                current = current.transform.parent.gameObject;
            }
        }
        #endregion

        public static Canvas GetCanvas(this RectTransform transform) => transform.GetComponentInParent<Canvas>();

        public static Vector2 CalculateSize(this RectTransform transform)
        {
            var anchorDelta = transform.anchorMax - transform.anchorMin;
            var parentSize = ((RectTransform)transform.parent)?.CalculateSize() ?? transform.GetCanvas().pixelRect.size;
            var size = anchorDelta * parentSize;
            if (size.x == 0) size.x = transform.sizeDelta.x;
            if (size.y == 0) size.y = transform.sizeDelta.y;

            return size;
        }

        #region Humanize Input Binding Key
        private static string HumanizeXBoxPaths(string path)
        {
            switch (path)
            {
                case "BUTTONNORTH":
                    return "Y";
                case "BUTTONSOUTH":
                    return "A";
                case "BUTTONWEST":
                    return "X";
                case "BUTTONEAST":
                    return "B";
                case "RIGHTSTICKPRESS":
                    return "R3";
                case "LEFTSTICKPRESS":
                    return "L3";
                case "LEFTSTICK":
                    return "L-STICK";
                case "RIGHTSTICK":
                    return "R-STICK";
                case "RIGHTSHOULDER":
                    return "R1";
                case "RIGHTTRIGGER":
                    return "R2";
                case "LEFTSHOULDER":
                    return "L1";
                case "LEFTTRIGGER":
                    return "L2";
                case "UP":
                    return "D-UP";
                case "DOWN":
                    return "D-DOWN";
                case "LEFT":
                    return "D-LEFT";
                case "RIGHT":
                    return "D-RIGHT";
                case "START":
                    return "≡";
            }

            return path;
        }

        private static string HumanizePSPaths(string path)
        {
            var xbox = HumanizeXBoxPaths(path);
            switch (xbox)
            {
                case "X":
                    return "□";
                case "Y":
                    return "△";
                case "B":
                    return "◯";
                case "A":
                    return "x";
                default:
                    return xbox;
            }
        }

        private static string HumanizeKeyboardPath(string path)
        {
            switch (path)
            {
                case "{SUBMIT}":
                    return "ENTER";
                case "BACKSPACE":
                    return "BKSP";
                case "RIGHTBUTTON":
                    return "RMB";
                case "LEFTBUTTON":
                    return "LMB";
                case "MIDDLEBUTTON":
                    return "MMB";
                default:
                    return path
                        .Replace("NUMPAD", "NUM")
                        .Replace("ARROW", "");
            }
        }

        public static string HumanizePath(string path, string unbound = null, SimplifiedDevice device = SimplifiedDevice.MouseAndKeyboard)
        {
            if (string.IsNullOrEmpty(path)) return unbound;

            var part = path
                 .Split('/')
                 .LastOrDefault();

            if (part == null) return unbound;

            var raw = part.ToUpper();

            switch (device)
            {
                case SimplifiedDevice.MouseAndKeyboard:
                    return HumanizeKeyboardPath(raw);
                case SimplifiedDevice.XBoxController:
                case SimplifiedDevice.SwitchController:
                case SimplifiedDevice.OtherController:
                    return HumanizeXBoxPaths(raw);
                case SimplifiedDevice.PSController:
                    return HumanizePSPaths(raw);
                default:
                    return raw;
            }
        }

        public static string HumanizePath(this InputBinding binding, string unbound = null) =>
            HumanizePath(binding.effectivePath, unbound);

        #endregion

        #region Find By Interface
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

        public static T FindObjectByInterface<T>() where T : class =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First();
        public static T FindObjectByInterfaceOrDefault<T>() where T : class =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault();

        public static T FindObjectByInterfaceOrDefault<T>(Func<T, bool> filter) where T : class =>
            FindObjectsByInterface<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(filter).FirstOrDefault();

        #endregion

        #region Scene
        /// <summary>
        /// Get first instance of type in scene of the behaviour
        /// </summary>
        public static T GetFirstInScene<T>(this MonoBehaviour mono) => mono.GetFirstInScene<T>(mono.gameObject.scene.name);

        /// <summary>
        /// Get first instance in scene
        /// </summary>
        /// <param name="sceneName">Name of scene</param>
        public static T GetFirstInScene<T>(this MonoBehaviour _, string sceneName) =>
            SceneManager
                .GetSceneByName(sceneName)
                .GetRootGameObjects()
                .Select(obj => obj.GetComponentInChildren<T>())
                .Where(t => t != null)
                .FirstOrDefault();

        /// <summary>
        /// Get first instance in scene
        /// </summary>
        /// <param name="sceneName">Name of scene</param>
        public static T GetFirstInScene<T>(this MonoBehaviour _, Scene scene) =>
            scene
                .GetRootGameObjects()
                .Select(obj => obj.GetComponentInChildren<T>())
                .Where(t => t != null)
                .FirstOrDefault();

        /// <summary>
        /// Get first instance in scene
        /// </summary>
        /// <param name="sceneName">Name of scene</param>
        public static T GetFirstInScene<T>(this MonoBehaviour _, Scene scene, Func<T, bool> filter) =>
            scene
                .GetRootGameObjects()
                .Select(obj => obj.GetComponentInChildren<T>())
                .Where(t => t != null && filter(t))
                .FirstOrDefault();
        #endregion

        /// <summary>
        /// Return inactive item out of list and activate it or instanciate a new item from the prefab
        /// and put it into the list
        /// </summary>
        public static T GetInactiveOrInstantiate<T>(
            this List<T> list,
            T prefab,
            System.Action<T> instanciationSetup = null) where T : MonoBehaviour
        {
            var recylced = list.FirstOrDefault(item => !item.gameObject.activeSelf);
            if (recylced != null)
            {
                recylced.gameObject.SetActive(true);
                return recylced;
            }

            var instance = GameObject.Instantiate(prefab);
            list.Add(instance);
            instanciationSetup?.Invoke(instance);

            return instance;
        }

        /// <summary>
        /// Populate list with specified number of copies of the prefab, all set as inactive
        /// </summary>
        public static void WarmUpFromPrefabs<T>(
            this List<T> list,
            T prefab,
            int count,
            System.Action<T> instanciationSetup = null
        ) where T : MonoBehaviour
        {
            for (int i = 0; i < count; i++)
            {
                var instance = GameObject.Instantiate(prefab);
                instance.gameObject.SetActive(false);
                instanciationSetup?.Invoke(instance);

                list.Add(instance);
            }
        }
    }
}