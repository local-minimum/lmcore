using LMCore.Extensions;
using LMCore.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.UI
{
    public class KeyBindingHint : MonoBehaviour
    {
        [SerializeField]
        Binding binding;

        [SerializeField]
        TextMeshProUGUI TextHint;

        [SerializeField]
        string bindingPattern = "[<color=\"red\">%KEY%</color>]";

        [SerializeField]
        string noBinding = "[<color=\"grey\">N/A</color>]";

        private string _dynamicText;
        public string DynamicText
        {
            get => _dynamicText ?? "";
            set
            {
                _dynamicText = value;
                if (inputBindingPath == null)
                {
                    SetBindingPathFromControls();
                }
                SyncDisplay();
            }
        }

        void SetBindingPathFromControls()
        {
            if (binding == null) return;

            var action = binding.PrimaryBinding(controls);
            inputBindingPath = InputBindingPathFromAction(action, controls);
        }

        private void Awake()
        {
            ActionMapToggler.OnChangeControls += ActionMapToggler_OnChangeControls;
            ActionMapToggler.OnToggleActionMap += ActionMapToggler_OnToggleActionMap;
        }

        private void OnDestroy()
        {
            ActionMapToggler.OnChangeControls -= ActionMapToggler_OnChangeControls;
            ActionMapToggler.OnToggleActionMap -= ActionMapToggler_OnToggleActionMap;
        }

        private void ActionMapToggler_OnToggleActionMap(
            PlayerInput input, InputActionMap enabled, InputActionMap disabled, SimplifiedDevice device)
        {
            if (binding == null || input == null) return;

            var bind = binding.PrimaryBinding(enabled, input.currentControlScheme);
            SyncDisplay(bind, input.currentControlScheme, device);
        }



        private void ActionMapToggler_OnChangeControls(PlayerInput input, string controls, SimplifiedDevice device)
        {
            if (binding == null) return;

            var bind = binding.PrimaryBinding(controls);
            SyncDisplay(bind, controls, device);
        }

        SimplifiedDevice device = SimplifiedDevice.MouseAndKeyboard;
        string controls;
        string inputBindingPath;

        string InputBindingPathFromAction(InputAction action, string controls = null) =>
            action == null ? null :
             action.bindings.FirstOrDefault(b =>
                b != null && b.groups != null && (controls == null || b.groups.Contains(controls))).path;


        void SyncDisplay(InputAction action, string controls, SimplifiedDevice device)
        {
            this.controls = controls;
            this.device = device;
            if (action == null)
            {
                inputBindingPath = null;
                SetBindingPathFromControls();
            }
            else
            {
                inputBindingPath = InputBindingPathFromAction(action, controls);
            }
            SyncDisplay();
        }

        void SyncDisplay()
        {
            if (string.IsNullOrEmpty(inputBindingPath))
            {
                TextHint.text = noBinding.Replace("%TEXT%", DynamicText);
            }
            else
            {
                var hint = UnityExtensions.HumanizePath(inputBindingPath, device: device);
                TextHint.text = bindingPattern.Replace("%KEY%", hint).Replace("%TEXT%", DynamicText);
            }
            Debug.Log($"KeyHint '{name}' {binding} {device} '{controls}': Path({inputBindingPath}) =>'{TextHint.text}'");
        }
    }
}
