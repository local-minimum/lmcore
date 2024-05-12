using LMCore.IO;
using UnityEngine;

public class KeybindingPresetButton : MonoBehaviour
{
    [SerializeField]
    KeybindingUI bindingUI;

    [SerializeField]
    string forward;
    [SerializeField]
    string backward;
    [SerializeField]
    string left;
    [SerializeField]
    string right;
    [SerializeField]
    string turnCW;
    [SerializeField]
    string turnCCW;

    void ApplySetting(Movement movement, string keyBinding)
    {
        var settings = GameSettings.GetMovementSetting(movement);

        if (settings != null)
        {
            settings.Value = keyBinding;
            bindingUI.RemapAction(movement, keyBinding);
        }

    }

    public void Click()
    {
        ApplySetting(Movement.Forward, forward);
        ApplySetting(Movement.Backward, backward);
        ApplySetting(Movement.StrafeLeft, left);
        ApplySetting(Movement.StrafeRight, right);
        ApplySetting(Movement.TurnCW, turnCW);
        ApplySetting(Movement.TurnCCW, turnCCW);
    }
}
