using LMCore.AbstractClasses;
using LMCore.Extensions;
using LMCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.UI
{
    public class MovementKeybindingUI : Singleton<MovementKeybindingUI, MovementKeybindingUI>
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
            public GamePlayAction action;
            public string defaultKey;

            public bool IsMovement => movement != Movement.None;
            public bool IsAction => action != GamePlayAction.None;

            public GameSettings.StringSetting Setting
            {
                get
                {
                    if (IsMovement) return GameSettings.GetMovementSetting(movement, defaultKey);
                    if (IsAction) return GameSettings.GetActionSettings(action, defaultKey).FirstOrDefault();
                    return null;
                }
            }

            public override string ToString()
            {
                if (IsMovement) return $"[{name} -> {movement}]";
                if (IsAction) return $"[{name} -> {action}]";
                return $"[{name} -> **VOID** ]";
            }
        }

        [Header("Configuration")]
        [SerializeField]
        PlayerInput playerInput;

        [SerializeField]
        string actionMapFilter = "Crawling";

        [SerializeField]
        string bindingGroup = "Keyboard";

        [SerializeField]
        string cancelButton = "escape";

        [SerializeField]
        int bindingIndex;
        public InputBinding GetActiveBinding(InputAction action) =>
            action.bindings[bindingIndex];

        [Header("Bindings")]
        [SerializeField]
        List<KeyBinding> bindings = new List<KeyBinding>();

        [SerializeField]
        SimpleButtonGroup ButtonGroup;


        #region Update Button Text
        void SetButtonTextFromPath(SimpleButton button, string path)
        {
            button.Text = UnityExtensions.HumanizePath(path) ?? "";
        }

        void SetButtonText(SimpleButton button, InputBinding binding, bool actionBound = true)
        {
            if (!actionBound)
            {
                Debug.LogWarning($"No binding for {button.name}");
                button.Text = "";
                return;
            }

            Debug.Log($"Syncing {button.name}: {binding.effectivePath}");
            button.Text = binding.HumanizePath() ?? "";
        }

        [Header("Rebinding effects")]
        [SerializeField]
        string rebindingText = "_";

        [SerializeField]
        bool blinkWhileRebinding = true;

        [SerializeField, Range(0, 5)]
        float rebindingBlinkFrequency = 1.0f;

        bool rebinding;
        SimpleButton rebindingButton;
        float rebindingBlink;

        void SetButtonRebinding()
        {
            if (!rebinding || rebindingButton == null) return;

            if (Time.timeSinceLevelLoad > rebindingBlink)
            {
                rebindingButton.Text = rebindingButton.Text == rebindingText ? "" : rebindingText;
                rebindingBlink = Time.timeSinceLevelLoad + rebindingBlinkFrequency;
            }
        }

        private void SyncButtonWithPath(KeyBinding binding, string path)
        {
            if (binding == null) { 
                Debug.LogWarning("No binding supplied");
                return;
            } else if (binding.button == null)
            {
                Debug.LogWarning($"Binding {binding} doesn't have a button");
                return;
            }

            Debug.Log($"Syncing {binding} as {path}");
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

        public IEnumerable<InputAction> GetActions(GamePlayAction action) =>
            GetBindings(action)
            .Select(b => GetAction(b))
            .Where(a => a != null);

        public InputAction GetAction(GamePlayAction action) =>
            GetActions(action)
            .FirstOrDefault();

        [SerializeField]
        string unboundHint = "<UNBOUND>"; 

        public string GetActionHint(GamePlayAction gpaction)
        {
            var keyHint = unboundHint;
            var action = GetAction(gpaction);
            if (action == null) return keyHint;

            if (action != null)
            {
                var binding = GetActiveBinding(action);
                if (binding != null)
                {
                    var bindingText = binding.HumanizePath();
                    if (bindingText != null)
                    {
                        keyHint = $"[{bindingText}]";
                    }
                }
            }
            return keyHint;
        }


        KeyBinding GetBinding(InputAction action) => bindings.FirstOrDefault(b => b.name == action.name || b.name == action.id.ToString());
        KeyBinding GetBinding(Movement movement) => bindings.FirstOrDefault(b => b.movement == movement);
        KeyBinding GetBinding(GamePlayAction action) => bindings.FirstOrDefault(b => b.action == action);
        IEnumerable<KeyBinding> GetBindings(GamePlayAction action) => bindings.Where(b => (b.action & action) == action);

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
            var setting = binding.Setting;
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
                var setting = binding.Setting;
                if (setting == null || registeredCallbacks.ContainsKey(setting)) { continue; }

                GameSettings.StringSetting.OnChangeEvent callback = newValue => SyncButtonWithPath(binding, newValue);

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
            var binding = GetBinding(movement);
            if (binding == null)
            {
                Debug.LogError($"Failed to set {movement} -> {key} binding because movement not known");
                return;
            }

            var action = GetAction(binding);

            action.ApplyBindingOverride(bindingIndex, $"<{bindingGroup}>/{key}");
        }

        public void RemapAction(GamePlayAction action, string key)
        {
            var binding = GetBinding(action);
            if (binding == null)
            {
                Debug.LogError($"Failed to set {action} -> {key} binding because action not known");
                return;
            }

            var inputAction = GetAction(binding);

            inputAction.ApplyBindingOverride(bindingIndex, $"<{bindingGroup}>/{key}");
        }

        private void RemapAction(SimpleButton button, InputAction actionToRebind, KeyBinding binding)
        {
            if (rebinding) { return; }

            rebinding = true;
            rebindingButton = button;
            rebindingBlink = -1f;

            ButtonGroup.Interactable = false;

            button.Selected();

            SetButtonRebinding();

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
                var setting = binding.Setting;

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
                var setting = binding.Setting;
                if (setting == null) continue;

                setting.RestoreDefault();
            }

            SyncActions();
        }

        private void Update()
        {
            SetButtonRebinding();
        }
    }
}
