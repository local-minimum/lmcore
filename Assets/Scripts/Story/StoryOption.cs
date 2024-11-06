using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StoryOption : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI ShadowText;
    [SerializeField]
    TextMeshProUGUI OptionText;
    Button Button;

    string PrefixLogMessage(string message) =>
        $"StoryOption: {message}";

    public void Configure(Choice choice, UnityAction onSelect)
    {
        Debug.Log(PrefixLogMessage($"{choice.index}: {choice.text}"));

        if (Button == null)
        {
            Button = GetComponentInChildren<Button>();
        }

        ShadowText.text = choice.text;
        OptionText.text = choice.text;

        Button.onClick.RemoveAllListeners();

        if (onSelect != null)
        {
            Button.onClick.AddListener(onSelect);
            Button.interactable = true;
        }
        else
        {
            Button.interactable = false;
        }
    }
}
