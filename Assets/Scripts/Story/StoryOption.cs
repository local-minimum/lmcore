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

    public void Configure(string optionText, UnityAction onSelect)
    {
        if (Button == null)
        {
            Button = GetComponentInChildren<Button>();
        }

        ShadowText.text = optionText;
        OptionText.text = optionText;

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
