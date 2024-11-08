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

        string ActiveText => showingText ?
            prompts.FirstOrDefault(prompt => !string.IsNullOrEmpty(prompt.text))?.text : null;

        string PrefixLogMessage(string message) =>
            $"PromptUI {(animator == null ? "simple" : "animated")} ({ActiveText}): {message}";

        private void Start()
        {
            if (!showingText)
                transform.HideAllChildren();
        }

        bool showingText;
        public void ShowText(string text)
        {
            Debug.Log(PrefixLogMessage($"Showing prompt: {text}"));
            showingText = true;
            foreach (var prompt in prompts)
            {
                prompt.text = text;
            }
            transform.ShowAllChildren();
            if (animator != null)
            {
                animator.ResetTrigger(hideTrigger);
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

        string hideableText;

        public void HideText(string text)
        {
            if (ActiveText == text)
            {
                Debug.Log(PrefixLogMessage($"Hiding prompt: {text}"));
                hideableText = text;
                if (animator != null)
                {
                    animator.SetTrigger(hideTrigger);
                    animator.ResetTrigger(showTrigger);
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
            if (ActiveText == hideableText)
            {
                Debug.Log(PrefixLogMessage($"Hiding UI: {hideableText}"));
                transform.HideAllChildren();
            }
        }
    }
}