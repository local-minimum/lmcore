using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using LMCore.AbstractClasses;
using LMCore.Extensions;

namespace LMCore.UI
{
    public class PromptUI : Singleton<PromptUI>
    {
        [SerializeField]
        TextMeshProUGUI prompt;

        private void Start()
        {
            transform.HideAllChildren();
        }

        public void ShowText(string text)
        {
            prompt.text = text;
            transform.ShowAllChildren();
        }

        public void HideText(string text)
        {
            if (prompt.text == text)
            {
                transform.HideAllChildren();
            }
        }
    }
}
