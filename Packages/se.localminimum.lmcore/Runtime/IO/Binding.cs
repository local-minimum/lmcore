using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.IO
{
    public class Binding : MonoBehaviour
    {
        [SerializeField]
        InputActionAsset inputAsset;

        [SerializeField]
        SerializableDictionary<string, string> maps = new SerializableDictionary<string, string>();

        [SerializeField]
        SerializableDictionary<string, string> actions = new SerializableDictionary<string, string>();

        /// <summary>
        /// This is used in the property drawer to give better feedback
        /// if for some reason a configured action no longer exists in the asset
        /// </summary>
        [SerializeField]
        SerializableDictionary<string, string> actionNames = new SerializableDictionary<string, string>();

        [ContextMenu("Info")]
        void Info()
        {
            foreach (var map in inputAsset.actionMaps)
            {
                Debug.Log($"{map.id} -> {map.name}");
            }
        }

        /// <summary>
        /// Get first/primary binding associated with currently active action map and
        /// for the specified control schema.
        ///
        /// Note: If schema is omitted it returns first binding for the action
        /// </summary>
        public InputAction GetInputAction(
            InputActionMap activeMap,
            string currentControlScheme = null,
            int bindingIndex = 0)
        {
            if (activeMap == null) return PrimaryBinding(currentControlScheme);

            if (!maps.Keys.Contains(activeMap.id.ToString())) return null;

            return actions
                .Where(a => a.Value == activeMap.id.ToString())
                .Select(kvp => activeMap.actions.FirstOrDefault(a => a.id.ToString() == kvp.Key))
                .Where(a => a != null &&
                    a.bindings.Any(b => string.IsNullOrEmpty(currentControlScheme) ||
                        (b.groups != null && b.groups.Contains(currentControlScheme))))
                .Skip(bindingIndex)
                .FirstOrDefault();

        }

        /// <summary>
        /// Get first/primary binding associated with any action map and
        /// for the specified control schema.
        ///
        /// Note: If schema is omitted it returns first binding for the action
        /// </summary>
        public InputAction GetInputAction(string currentControlScheme = null, int bindingIndex = 0)
        {
            return actions
                .Select(kvp => inputAsset.actionMaps.SelectMany(am => am.actions).FirstOrDefault(a => a.id.ToString() == kvp.Key))
                .Where(a => a != null &&
                    a.bindings.Any(b => string.IsNullOrEmpty(currentControlScheme) ||
                        (b.groups != null && b.groups.Contains(currentControlScheme))))
                .Skip(bindingIndex)
                .FirstOrDefault();

        }

        /// <summary>
        /// Get first/primary binding associated with currently active action map and
        /// for the specified control schema.
        ///
        /// Note: If schema is omitted it returns first binding for the action
        /// </summary>
        public InputAction PrimaryBinding(InputActionMap activeMap, string currentControlScheme = null)
        {
            if (activeMap == null) return PrimaryBinding(currentControlScheme);

            if (!maps.Keys.Contains(activeMap.id.ToString())) return null;

            return actions
                .Where(a => a.Value == activeMap.id.ToString())
                .Select(kvp => activeMap.actions.FirstOrDefault(a => a.id.ToString() == kvp.Key))
                .FirstOrDefault(a => a != null &&
                    a.bindings.Any(b => string.IsNullOrEmpty(currentControlScheme) ||
                        (b.groups != null && b.groups.Contains(currentControlScheme))));

        }

        /// <summary>
        /// Get first/primary binding associated with any action map and
        /// for the specified control schema.
        ///
        /// Note: If schema is omitted it returns first binding for the action
        /// </summary>
        public InputAction PrimaryBinding(string currentControlScheme = null)
        {
            return actions
                .Select(kvp => inputAsset.actionMaps.SelectMany(am => am.actions).FirstOrDefault(a => a.id.ToString() == kvp.Key))
                .FirstOrDefault(a => a != null &&
                    a.bindings.Any(b => string.IsNullOrEmpty(currentControlScheme) ||
                        (b.groups != null && b.groups.Contains(currentControlScheme))));

        }

        public override string ToString()
        {
            var bindings = string.Join(
                ", ",
                maps.Select(m => $"<{m.Value}: {string.Join(", ", actions.Where(a => a.Value == m.Key).Select(a => actionNames.FirstOrDefault(an => an.Key == a.Key).Value))}>")
            );
            return $"[Binding: {bindings}]";
        }
    }
}
