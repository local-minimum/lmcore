using LMCore.Extensions;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.IO
{
    [RequireComponent(typeof(Binding))]
    public abstract class AbsBindingConf : MonoBehaviour
    {
        [SerializeField]
        string bindingGroup = "Keyboard";
        public string BindingGroup => bindingGroup;

        public bool For(SimplifiedDevice device)
        {
            switch (device)
            {
                case SimplifiedDevice.PSController:
                case SimplifiedDevice.OtherController:
                case SimplifiedDevice.SwitchController:
                case SimplifiedDevice.XBoxController:
                    return bindingGroup == "GamePad";
                case SimplifiedDevice.MouseAndKeyboard:
                    return bindingGroup == "Keyboard" || bindingGroup == "Mouse";
                default:
                    return false;
            }
        }

        [SerializeField]
        protected int bindingIndex;
        public int BindingIndex => bindingIndex;

        protected Binding binding => GetComponent<Binding>();

        public InputAction action
        {
            get
            {
                if (binding == null) return null;
                return binding.GetInputAction(bindingGroup);
            }
        }

        public InputBinding inputBinding
        {
            get
            {
                var a = action;
                if (a == null || a.bindings.Count <= bindingIndex)
                {
                    Debug.LogWarning($"No valid binding for {action} and index {bindingIndex}");
                    return new InputBinding();
                }

                return a.bindings
                    .Where(b => b.groups == null || b.groups.Contains(bindingGroup))
                    .Skip(bindingIndex)
                    .FirstOrDefault();
            }
        }

        abstract public GameSettings.StringSetting Settings { get; }

        public string HumanizedBinding()
        {
            return inputBinding.HumanizePath();
        }

        public string ActivePath => inputBinding.effectivePath;

        public void RevertBindingOverride()
        {
            action.RemoveBindingOverride(bindingIndex);
        }

        private void Awake()
        {
            var setting = Settings;
            Debug.Log($"Settings are: {setting}");

            if (setting != null && !string.IsNullOrEmpty(setting.Value))
            {
                var wantedPath = setting.Value;
                var activePath = ActivePath;
                if (wantedPath != activePath)
                {
                    Debug.Log($"Updating {action} path to {wantedPath} (was {activePath}) from settings");
                    action.ApplyBindingOverride(bindingIndex, wantedPath);
                }
                else
                {
                    Debug.Log($"Active and wanted path match: {activePath} == {wantedPath}");
                }
            }
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"Binding {binding} binding #{bindingIndex} <{bindingGroup}> {(action == null ? "missing" : inputBinding)} and saved setting {Settings}");
        }
    }
}
