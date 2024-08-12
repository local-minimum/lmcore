using LMCore.AbstractClasses;
using LMCore.Extensions;
using TMPro;
using UnityEngine;

namespace LMCore.UI
{
    public class PromptUI : Singleton<PromptUI, PromptUI>
    {
        [SerializeField]
        private TextMeshProUGUI prompt;

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