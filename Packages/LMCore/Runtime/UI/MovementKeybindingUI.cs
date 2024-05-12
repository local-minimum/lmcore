using LMCore.AbstractClasses;
using LMCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.UI
{
    public class MovementKeybindingUI : Singleton<MovementKeybindingUI>
    {
        [Serializable]
        class KeyBinding
        {
            /// <summary>
            /// Name or GUID of InputAction
            /// </summary>
            public string name;
            public SimpleButton button;
            public Movement movement;
        }

        [SerializeField]
        PlayerInput playerInput;

        [SerializeField]
        string actionMapFilter = "Crawling";

        [SerializeField]
        string bindingGroup = "Keyboard";

        [SerializeField]
        string cancelButton = "escape";

        [SerializeField]
        List<KeyBinding> bindings = new List<KeyBinding>();

        [SerializeField]
        SimpleButtonGroup ButtonGroup;

        [SerializeField]
        int bindingIndex;

        #region Update Button Text
        void SetButtonText(SimpleButton button, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                button.Text = "";
            }
            else
            {
                button.Text = text.ToUpper().Replace("NUMPAD", "NUM").Replace("ARROW", "");
            }
        }

        void SetButtonTextFromPath(SimpleButton button, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                button.Text = "";
            }
            SetButtonText(button, path.Split('/').LastOrDefault());
        }

        void SetButtonText(SimpleButton button, InputBinding binding, bool actionBound = true)
        {
            if (!actionBound)
            {
                Debug.LogWarning($"No binding for {button.name}");
                SetButtonText(button, null);
                return;
            }

            Debug.Log($"Syncing {button.name}: {binding.effectivePath}");
            SetButtonTextFromPath(button, binding.effectivePath);
        }

        void SetButtonRebinding(SimpleButton button)
        {
            button.Text = "...";
        }

        private void SyncButtonWithPath(Movement movement, string path)
        {
            var binding = bindings.FirstOrDefault(b => b.movement == movement);
            if (binding == null || binding.button == null)
            {
                Debug.LogWarning($"Could not find a button for movement {movement}");
                return;
            }

            Debug.Log($"Syncing {movement} as {path}");
            SetButtonTextFromPath(binding.button, path);
        }
        #endregion Update Button Text

        List<InputAction> actions
        {
            get
            {
                if (playerInput == null) { return new List<InputAction>(); }

                return playerInput.actions
                    .Where(a => a.actionMap.name == actionMapFilter)
                    .ToList();
            }
        }

        InputAction GetAction(KeyBinding binding) => actions.FirstOrDefault(a => a.name == binding.name || a.id.ToString() == binding.name);
        public InputAction GetAction(Movement movement)
        {
            var binding = GetBinding(movement);
            if (binding == null) return null;
            return GetAction(binding);
        }

        KeyBinding GetBinding(InputAction action) => bindings.FirstOrDefault(b => b.name == action.name || b.name == action.id.ToString());
        KeyBinding GetBinding(Movement movement) => bindings.FirstOrDefault(b => b.movement == movement);

        #region Syncing
        /// <summary>
        /// Extends serialized bindings with those missing from the group
        /// </summary>
        void PopulateUnassigned(List<InputAction> actions)
        {
            var unassigned = actions
                .Where(a => !bindings.Any(b => b.name == a.name || b.name == a.id.ToString()));

            foreach (var binding in unassigned)
            {
                bindings.Add(new KeyBinding { name = binding.name });
            }
        }

        /// <summary>
        /// Applies stored/default key binding setting to input action
        /// </summary>
        void SyncInputBindingWithSettings(InputAction action, KeyBinding binding)
        {
            var setting = GameSettings.GetMovementSetting(binding.movement);
            if (setting != null)
            {
                if (string.IsNullOrEmpty(setting.Value))
                {
                    action.Disable();
                }
                else
                {
                    action.ApplyBindingOverride(bindingIndex, $"<{bindingGroup}>/{setting.Value}");
                    action.Enable();
                }
            }
            else
            {
                Debug.LogWarning($"No movement setting availabled for {binding.movement}");
            }
        }

        /// <summary>
        /// Syncs actions and binding settings
        /// </summary>
        void SyncActions()
        {
            PopulateUnassigned(actions);

            foreach (var action in actions)
            {
                var binding = GetBinding(action);
                if (binding == null || binding.button == null)
                {
                    if (binding == null)
                    {
                        Debug.LogWarning($"No binding found for {action.name}/{action.id}");
                    }
                    else
                    {
                        Debug.LogWarning($"Binding {binding.name}/{binding.movement} has no button assigned");
                    }
                    continue;
                }

                binding.button.DeSelect();

                SyncInputBindingWithSettings(action, binding);
                SetButtonText(binding.button, action.bindings[bindingIndex], action.enabled);
            }
        }
        #endregion Syncing

        private Dictionary<GameSettings.StringSetting, GameSettings.StringSetting.OnChangeEvent> registeredCallbacks = new Dictionary<GameSettings.StringSetting, GameSettings.StringSetting.OnChangeEvent>();

        #region Setup and teardown
        void RegisterCallbacks()
        {
            foreach (var binding in bindings)
            {
                var setting = GameSettings.GetMovementSetting(binding.movement);
                if (setting == null || registeredCallbacks.ContainsKey(setting)) { continue; }

                GameSettings.StringSetting.OnChangeEvent callback = newValue => SyncButtonWithPath(binding.movement, newValue);

                registeredCallbacks.Add(setting, callback);
                setting.OnChange += callback;
            }
        }

        void UnregisterCallbacks()
        {
            foreach (var (setting, callback) in registeredCallbacks)
            {
                setting.OnChange -= callback;
            }
        }


        new private void Awake()
        {
            base.Awake();

            RegisterCallbacks();
            SyncActions();

            Debug.Log(string.Join("\n", playerInput.actions.Select(SummarizeAction)));
        }

        new private void OnDestroy()
        {
            UnregisterCallbacks();
            base.OnDestroy();
        }
        #endregion Setup and teardown

        string SummarizeAction(InputAction action)
        {
            var bindings = string.Join(", ", action.bindings.Select(SummarizeBinding));
            return $"{action.name}: {action.id} ({bindings})";
        }

        string SummarizeBinding(InputBinding binding)
        {
            return $"{binding.name}/{binding.path}/{binding.effectivePath}";
        }

        bool rebinding;

        #region Rebinding
        /// <summary>
        /// Callback function for triggering remapping single binding
        /// </summary>
        public void RemapAction(SimpleButton button)
        {
            var binding = bindings.FirstOrDefault(b => b.button == button);
            if (binding == null)
            {
                Debug.LogWarning($"Button {button} has no configuration");
                return;
            }

            var action = GetAction(binding);

            if (action == null)
            {
                Debug.LogWarning($"There's a configuration for {button} but the sought action {binding.name} does not exist");
                return;
            }

            RemapAction(button, action, binding);
        }

        /// <summary>
        /// Interface for binding key from other script (e.g. keybinding presets)
        ///
        /// Note that the key should not be a path but just the stored key
        /// </summary>
        public void RemapAction(Movement movement, string key)
        {
            var binding = bindings.FirstOrDefault(b => b.movement == movement);
            if (binding == null)
            {
                Debug.LogError($"Failed to set {movement} -> {key} binding because movement not known");
            }

            var action = GetAction(binding);

            action.ApplyBindingOverride(bindingIndex, $"<{bindingGroup}>/{key}");
        }

        private void RemapAction(SimpleButton button, InputAction actionToRebind, KeyBinding binding)
        {
            if (rebinding) { return; }
            rebinding = true;
            ButtonGroup.Interactable = false;

            button.Selected();

            SetButtonRebinding(button);

            BlockableActions.BlockAction(this);

            actionToRebind.Disable();

            actionToRebind
                .PerformInteractiveRebinding(bindingIndex)
                .WithControlsHavingToMatchPath($"<{bindingGroup}>")
                .WithBindingGroup(bindingGroup)
                .WithCancelingThrough($"<{bindingGroup}>/{cancelButton}")
                .OnCancel(operation => CleanUp(button, operation.action, binding, false))
                .OnComplete(operation =>
                {
                    operation.Dispose();
                    CleanUp(button, operation.action, binding, true);
                })
                .Start();

        }

        void CleanUp(SimpleButton button, InputAction action, KeyBinding binding, bool rebound)
        {
            rebinding = false;
            if (rebound)
            {
                var setting = GameSettings.GetMovementSetting(binding.movement);

                if (setting == null)
                {
                    Debug.LogWarning($"No place to store {binding.movement}, setting will be transitory");
                    SetButtonText(button, action.bindings[bindingIndex]);
                }
                else
                {
                    var buttonPath = action.bindings[bindingIndex].effectivePath.Split('/').LastOrDefault();
                    setting.Value = buttonPath;
                }
            }
            else
            {
                SetButtonText(button, action.bindings[bindingIndex]);
            }

            action.Enable();

            button.DeSelect();
            ButtonGroup.Interactable = true;
            BlockableActions.RemoveActionBlock(this);
        }
        #endregion Rebinding

        public void RestoreDefaults()
        {
            foreach (var binding in bindings)
            {
                var setting = GameSettings.GetMovementSetting(binding.movement);
                if (setting == null) continue;

                setting.RestoreDefault();
            }

            SyncActions();
        }
    }
}
