using LMCore.AbstractClasses;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LMCore.UI
{
    public class PromptUI : Singleton<PromptUI, PromptUI>
    {
        [SerializeField]
        private List<TextMeshProUGUI> prompts = new List<TextMeshProUGUI>();

        [SerializeField]
        Animator animator;

        [SerializeField]
        string showTrigger;

        [SerializeField]
        string hideTrigger;

        private void Start()
        {
            if (!showingText)
                transform.HideAllChildren();
        }

        bool showingText;
        public void ShowText(string text)
        {
            showingText = true;
            foreach (var prompt in prompts)
            {
                prompt.text = text;
            }
            transform.ShowAllChildren();
            if (animator != null)
            {
                animator.SetTrigger(showTrigger);
            }
        }
        
        public void ShowText(string text, float duration)
        {
            ShowText(text);
            StartCoroutine(HideText(text, duration));
        }

        IEnumerator<WaitForSeconds> HideText(string text, float delay)
        {
            yield return new WaitForSeconds(delay);
            HideText(text);
        }

        public void HideText(string text)
        {
            if (prompts.Any(prompt => prompt.text == text))
            {
                if (animator != null)
                {
                    animator.SetTrigger(hideTrigger);
                }
                else
                {
                    transform.HideAllChildren();
                }
                showingText = false;
            }
        }

        public void HideAllChildren()
        {
            transform.HideAllChildren();
        }
    }
}