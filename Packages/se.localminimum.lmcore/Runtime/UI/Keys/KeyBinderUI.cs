using LMCore.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.UI
{
    public class KeyBinderUI : MonoBehaviour
    {
        [SerializeField]
        AbsBindingConf bindingConf;
        public AbsBindingConf Conf => bindingConf;

        [SerializeField]
        TextMeshProUGUI bindingText;

        [SerializeField]
        string cancelButton = "escape";

        bool rebinding;

        public void RebindAction()
        {
            if (bindingConf == null)
            {
                Debug.LogError($"No binding configured");
                return;
            }

            if (rebinding)
            {
                Debug.LogWarning($"Cannot rebind to actions at the same time");
                return;
            }

            rebinding = true;
            // TODO: Disable all buttons while rebinding
            AnimateRebinding();

            var actionToRebind = bindingConf.action;

            actionToRebind.Disable();
            actionToRebind
                .PerformInteractiveRebinding(bindingConf.BindingIndex)
                .WithControlsHavingToMatchPath($"<{bindingConf.BindingGroup}>")
                .WithBindingGroup(bindingConf.BindingGroup)
                .WithCancelingThrough($"<{bindingConf.BindingGroup}>/{cancelButton}")
                .OnCancel(operation => CleanUpRebind(operation.action, false))
                .OnComplete(operation =>
                {
                    operation.Dispose();
                    CleanUpRebind(operation.action, true);
                })
                .Start();
        }

        public void SetBindingOverride(string key)
        {
            bindingConf.action.ApplyBindingOverride(bindingConf.BindingIndex, $"<{bindingConf.BindingGroup}>/{key}");
            CleanUpRebind(bindingConf.action, true);
        }

        public void RemoveBindingOverride()
        {
            bindingConf.action.RemoveBindingOverride(bindingConf.BindingIndex);
            CleanUpRebind(bindingConf.action, true);
        }

        public void CleanUpRebind(InputAction action, bool rebound)
        {
            rebinding = false;
            if (rebound)
            {
                var setting = bindingConf.Settings;
                if (setting == null)
                {
                    Debug.LogWarning($"Storage of keybinding not configured");
                }
                else
                {
                    setting.Value = bindingConf.ActivePath;
                    Debug.Log($"Updated stored settings {setting}");
                }
            }

            SyncButtonText();
        }

        [ContextMenu("Sync button text")]
        void SyncButtonText()
        {
            if (bindingConf == null)
            {
                Debug.LogError("There's no binding");
                bindingText.text = "";
                return;
            }

            bindingText.text = bindingConf.HumanizedBinding();
        }

        private void OnEnable()
        {
            SyncButtonText();
        }

        float rebingingBlink;
        string rebindingText = "___";
        void AnimateRebinding()
        {
            if (!rebinding) return;


            if (Time.timeSinceLevelLoad > rebingingBlink)
            {
                bindingText.text = bindingText.text == rebindingText ? "" : rebindingText;
                rebingingBlink = Time.timeSinceLevelLoad + 1f;
            }
        }

        private void Update()
        {
            AnimateRebinding();
        }
    }
}
