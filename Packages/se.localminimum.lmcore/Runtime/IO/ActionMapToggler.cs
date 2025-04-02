using LMCore.AbstractClasses;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.IO
{

    public delegate void ChangeControlsEvent(PlayerInput input, string controlScheme, SimplifiedDevice device);
    public delegate void ToggleActionMapEvent(PlayerInput input, InputActionMap enabled, InputActionMap disabled, SimplifiedDevice device);

    public class ActionMapToggler : Singleton<ActionMapToggler, ActionMapToggler>
    {
        /// <summary>
        /// Fired when an action map gets swapped out for another
        /// </summary>
        public static event ToggleActionMapEvent OnToggleActionMap;

        /// <summary>
        /// Fired when control scheme changes (e.g. keyboard to controller)
        /// </summary>
        public static event ChangeControlsEvent OnChangeControls;

        InputActionMap enabledMap;
        List<PlayerInput> connectedInputs = new List<PlayerInput>();

        public static InputActionMap EnabledMap => instance.enabledMap;

        SimplifiedDevice lastDevice = SimplifiedDevice.MouseAndKeyboard;
        public static SimplifiedDevice LastDevice => instance.lastDevice;

        private void SetupPlayerInput(PlayerInput playerInput)
        {
            if (!connectedInputs.Contains(playerInput))
            {
                playerInput.controlsChangedEvent.AddListener(PlayerInput_onControlsChanged);
                connectedInputs.Add(playerInput);
            }
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            foreach (var connected in connectedInputs)
            {
                connected.onControlsChanged -= PlayerInput_onControlsChanged;
            }
        }

        public void ToggleByName(PlayerInput playerInput, string enable, string disable = null)
        {
            SetupPlayerInput(playerInput);

            if (playerInput == null)
            {
                Debug.LogError("ActionMapToggler: No player input in scene");
                return;
            }

            lastDevice = GetDevice(playerInput);

            var enableMap = playerInput.actions.FindActionMap(enable);
            var disableMap = disable == null ? null : playerInput.actions.FindActionMap(disable);

            if (enableMap != null)
            {
                enabledMap = enableMap;
                enableMap.Enable();
            }

            if (disableMap == enabledMap)
            {
                enabledMap = null;
            }

            if (disableMap != null)
            {
                disableMap.Disable();
            }

            OnToggleActionMap?.Invoke(playerInput, enabledMap, disableMap, lastDevice);
        }

        SimplifiedDevice GetDevice(PlayerInput input)
        {
            var name = input.devices.OrderByDescending(d => d.lastUpdateTime).FirstOrDefault().name;
            if (name == "Mouse" || name == "Keyboard") return SimplifiedDevice.MouseAndKeyboard;

            if (name.Contains("XInputController")) return SimplifiedDevice.XBoxController;
            if (name.Contains("DualShock")) return SimplifiedDevice.PSController;
            if (name.Contains("SwitchPro")) return SimplifiedDevice.SwitchController;
            if (input.currentControlScheme.Contains("Gamepad"))
            {
                Debug.LogWarning($"Unknown gamepad: {name}");
                return SimplifiedDevice.OtherController;
            }

            Debug.LogWarning($"Unknown device and control scheme: {name} / {input.currentControlScheme}");
            return SimplifiedDevice.MouseAndKeyboard;
        }

        private void PlayerInput_onControlsChanged(PlayerInput obj)
        {
            lastDevice = GetDevice(obj);
            Debug.Log($"ActionMapToggler: New controls are {lastDevice} & {obj.currentControlScheme}");
            OnChangeControls?.Invoke(obj, obj.currentControlScheme, lastDevice);
        }

        private void Start()
        {
            var pi = FindFirstObjectByType<PlayerInput>();
            if (pi != null)
            {
                SetupPlayerInput(pi);
                OnChangeControls?.Invoke(pi, pi.currentControlScheme, lastDevice);
            }
        }
    }
}
