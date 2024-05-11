using LMCore.AbstractClasses;
using LMCore.IO;
using LMCore.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindingUI : MonoBehaviour
{
    [Serializable]
    class KeyBinding
    {
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

    void SetButtonText(SimpleButton button, InputBinding binding)
    {
        Debug.Log($"Syncing {button.name}: {binding.effectivePath}");
        var text = binding.effectivePath.Split('/').LastOrDefault();
        button.Text = text.ToUpper();
    }

    void SyncActions()
    {
        if (playerInput == null) { return; }

        var actions = playerInput.actions
            .Where(a => a.actionMap.name == actionMapFilter)
            .ToList();

        var unassigned = actions
            .Where(a => !bindings.Any(b => b.name == a.name || b.name == a.id.ToString()));

        foreach ( var binding in unassigned )
        {
            bindings.Add(new KeyBinding { name = binding.name });
        }

        foreach ( var action in actions)
        {
            var binding = bindings.FirstOrDefault(b => b.name == action.name || b.name == action.id.ToString());
            if (binding == null || binding.button == null) { continue; }

            binding.button.DeSelect();

            var setting = GameSettings.GetMovementSetting(binding.movement);
            if (setting != null)
            {
                action.ApplyBindingOverride(bindingIndex, $"<{bindingGroup}>/{setting.Value}");
            }
            SetButtonText(binding.button, action.bindings[bindingIndex]);
        }
    }

    private Dictionary<GameSettings.StringSetting, GameSettings.StringSetting.OnChangeEvent> registeredCallbacks =  new Dictionary<GameSettings.StringSetting, GameSettings.StringSetting.OnChangeEvent>();

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


    void Awake()
    {
        RegisterCallbacks();
        SyncActions();

        Debug.Log(string.Join("\n", playerInput.actions.Select(SummarizeAction)));
    }

    private void OnDestroy()
    {
        UnregisterCallbacks(); 
    }

    private void SyncButtonWithPath(Movement movement, string path)
    {
        var binding = bindings.FirstOrDefault(b => b.movement == movement);
        if (binding == null)
        {
            Debug.LogWarning($"Could not find a button for movement {movement}");
            return;
        }

        Debug.Log($"Syncing {movement} as {path}");
        if (string.IsNullOrEmpty(path))
        {
            binding.button.Text = "";
        } else
        {
            var text = path.Split('/').LastOrDefault();
            binding.button.Text = text.ToUpper();
        }
    }

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


    void SetButtonRebinding(SimpleButton button)
    {
        button.Text = "...";
    }

    public void RemapAction(SimpleButton button)
    {
        var binding = bindings.FirstOrDefault(b => b.button == button);
        if (binding == null)
        {
            Debug.LogWarning($"Button {button} has no configuration");
            return;
        }

        var action = playerInput.actions
            .Where(a => a.actionMap.name == actionMapFilter)
            .FirstOrDefault(a => binding.name == a.name || binding.name == a.id.ToString());

        if (action == null)
        {
            Debug.LogWarning($"There's a configuration for {button} but the sought action {binding.name} does not exist");
            return;
        }

        RemapAction(button, action, binding);
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
            .OnComplete(operation => {
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
            } else
            {
                var buttonPath = action.bindings[bindingIndex].effectivePath.Split('/').LastOrDefault();
                setting.Value = buttonPath;
            }
        } else
        {
            SetButtonText(button, action.bindings[bindingIndex]);
        }

        action.Enable();

        button.DeSelect();
        ButtonGroup.Interactable = true;
        BlockableActions.RemoveActionBlock(this);
    }

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
