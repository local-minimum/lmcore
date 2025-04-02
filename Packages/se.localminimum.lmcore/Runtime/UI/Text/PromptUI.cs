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

        struct QueueItem
        {
            public string text;
            public float? duration;
        }

        List<QueueItem> queue = new List<QueueItem>();

        /// <summary>
        /// Put text in queue, to be shown as soon as previous message disappears
        /// </summary>
        /// <param name="text">Text to be shown</param>
        /// <param name="duration">Duration text should be shown, if null shown untill removed</param>
        public void QueueText(string text, float? duration = null)
        {
            if (!showingText)
            {
                ShowText(text);
            }
            else
            {
                Debug.Log(PrefixLogMessage($"Enqueing prompt: {text} / {duration}s"));
                queue.Add(new QueueItem() { text = text, duration = duration });
            }
        }

        bool showingText;

        /// <summary>
        /// Force show a text no matter if there's already something showing
        /// </summary>
        /// <param name="text">Text to show</param>
        public void ShowText(string text)
        {
            if (showingText && !isTemporaryText)
            {
                var activeText = ActiveText;
                if (!string.IsNullOrEmpty(activeText))
                {
                    queue.Insert(0, new QueueItem() { text = ActiveText });
                }
            }

            Debug.Log(PrefixLogMessage($"Showing prompt: {text}"));
            isTemporaryText = false;
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

        bool isTemporaryText;

        public void ShowText(string text, float duration)
        {
            ShowText(text);
            StartCoroutine(DelayRemoveText(text, duration));
        }

        IEnumerator<WaitForSeconds> DelayRemoveText(string text, float delay)
        {
            isTemporaryText = true;
            yield return new WaitForSeconds(delay);
            RemoveText(text);
            isTemporaryText = false;
        }

        string hideableText;

        /// <summary>
        /// Removes text, either from queue or if it is showing
        /// </summary>
        /// <param name="text">The text to remove</param>
        public void RemoveText(string text)
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

            queue = queue.Where(item => item.text != text).ToList();

            CheckQueue();
        }

        void CheckQueue()
        {
            if (queue.Count == 0) return;
            var item = queue.First();
            queue.RemoveAt(0);

            if (item.duration == null)
            {
                ShowText(item.text);
            }
            else
            {
                ShowText(item.text, item.duration ?? 0f);
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