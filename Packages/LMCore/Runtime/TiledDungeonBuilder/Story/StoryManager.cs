using Ink.Runtime;
using LMCore.AbstractClasses;
using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.TiledDungeon.Narrative
{
    public enum StoryPhase { Start, End };

    public delegate void StoryPhaseEvent(StoryPhase phase, Story story);

    public class StoryManager : Singleton<StoryManager, StoryManager>
    {
        public static event StoryPhaseEvent OnStoryPhaseChange;

        private enum ShowTextMode { Instant, WordByWord, WordByWordlength, LetterByLetter };
        [SerializeField]
        ShowTextMode TextMode = ShowTextMode.Instant;

        [SerializeField, Range(0, 2), Tooltip("Delay between each piece shown")]
        float showStorySpeed = 0.1f;

        [SerializeField, Range(0, 1), Tooltip("Delay cost when doing word by word length per character")]
        float showStoryCharacterSpeedCost = 0.05f;

        [SerializeField]
        bool extraCostOnPunctuaton = true;

        [SerializeField, Range(0, 1), Tooltip("Delay cost when doing word by word length per punctuation")]
        float showStoryPunctuationCost = 0.2f;

        [SerializeField]
        string punctuationCharacters = ".,:-?!";

        [SerializeField]
        GameObject StoryUIRoot;

        [SerializeField]
        TextMeshProUGUI StoryUI;

        [SerializeField]
        Button NextButton;

        [SerializeField]
        RectTransform OptionsRoot;

        [SerializeField]
        StoryOption OptionPrefab;

        [SerializeField]
        float delayShowOptionsWith = 0.2f;

        List<StoryOption> Options = new List<StoryOption>();

        private void Start()
        {
            if (ActiveStory == null)
                StoryUIRoot.SetActive(false);
        }

        private void OnEnable()
        {
            StoryTrigger.OnPlayStory += StoryTrigger_OnPlayStory;
            GridEntity.OnInteract += GridEntity_OnInteract;
        }

        private void OnDisable()
        {
            StoryTrigger.OnPlayStory -= StoryTrigger_OnPlayStory;
            GridEntity.OnInteract -= GridEntity_OnInteract;
        }

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (entity != trigger?.InteractingEntity) return;

            if (AnimatingStory)
            {
                showStoryParts = null;
                ShowStoryInstant();
            } else if (NextButton.gameObject.activeSelf)
            {
                NextButton.onClick?.Invoke();
            }
        }

        Ink.Runtime.Story ActiveStory;
        public bool Playing => ActiveStory != null;

        string PrefixLogMessage(string message) =>
            $"StoryManager: {message}";

        StoryTrigger trigger;
        private void StoryTrigger_OnPlayStory(Ink.Runtime.Story story, StoryTrigger trigger, bool resume)
        {
            if (ActiveStory != null)
            {
                Debug.LogError(PrefixLogMessage($"We should never have two active stories, {trigger} gave me a new while one was playing. Switching."));
            }

            OnStoryPhaseChange?.Invoke(StoryPhase.Start, story);
            NextButton.onClick.AddListener(ContinueStory);

            ActiveStory = story;
            this.trigger = trigger;
            StoryUIRoot.SetActive(true);
            ContinueStory(resume);
        }

        bool CanContinueStory => ActiveStory != null && ActiveStory.canContinue;

        void SyncSimpleProgress()
        {
            Debug.Log(PrefixLogMessage("Hiding options area since there are none"));
            if (!ActiveStory.canContinue)
            {
                NextButton.onClick.RemoveListener(ContinueStory);
                NextButton.onClick.AddListener(EndStory);
            }

            NextButton.gameObject.SetActive(true);
        }

        void HideAllProgressOptions()
        {
            NextButton.gameObject.SetActive(false);
            OptionsRoot.gameObject.SetActive(false);
            for (int i = 0, l = Options.Count; i < l; i++)
            {
                Options[i].gameObject.SetActive(false);
            }
        }

        void SyncOptionsProgress()
        {
            Debug.Log(PrefixLogMessage("Showing options"));
            OptionsRoot.gameObject.SetActive(true);
            var nOptionsBefore = Options.Count;
            for (int i = 0, l = ActiveStory.currentChoices.Count; i < l; i++)
            {
                var choice = ActiveStory.currentChoices[i];
                StoryOption option = null;
                if (i < nOptionsBefore)
                {
                    option = Options[i];
                }
                else
                {
                    option = Instantiate(OptionPrefab, OptionsRoot);
                    Options.Add(option);
                }
                option.Configure(
                    choice,
                    () =>
                    {
                        Debug.Log($"Your choice index is {choice.index}");
                        ActiveStory.ChooseChoiceIndex(choice.index);
                        ContinueStory();
                    });

                if (delayShowOptionsWith > 0)
                {
                    StartCoroutine(DelayShowOption((choice.index + 1) * delayShowOptionsWith, option));
                } else
                {
                    option.gameObject.SetActive(true);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(OptionsRoot);
            var group = OptionsRoot.GetComponent<VerticalLayoutGroup>();
        }

        IEnumerator<WaitForSeconds> DelayShowOption(float delay, StoryOption option)
        {
            yield return new WaitForSeconds(delay);
            option.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(OptionsRoot);
        }

        void ShowProgressOptions()
        {
            if (ActiveStory.currentChoices.Count == 0)
            {
                SyncSimpleProgress();
            }
            else
            {
                SyncOptionsProgress();
            }
        }

        void ShowStoryInstant()
        {
            StoryUI.text = ActiveStory.currentText;
            HideAllProgressOptions();
            ShowProgressOptions();
        }

        int showStoryPartsIndex;
        string[] showStoryParts;

        void ShowStoryWordByWord()
        {
            HideAllProgressOptions();
            showStoryParts = ActiveStory.currentText.Split(" ");
            showStoryPartsIndex = 0;
            nextShowTime = Time.timeSinceLevelLoad;
            Debug.Log(PrefixLogMessage($"{showStoryParts.Length} parts to story"));
        }

        void ShowStoryLetterByLetter()
        {
            HideAllProgressOptions();
            showStoryParts = Regex.Split(ActiveStory.currentText, string.Empty);
            showStoryPartsIndex = 0;
            nextShowTime = Time.timeSinceLevelLoad;
            Debug.Log(PrefixLogMessage($"{showStoryParts.Length} parts to story"));
        }


        void ContinueStory() => ContinueStory(false);
        void ContinueStory(bool resume)
        {
            if (ActiveStory == null)
            {
                Debug.Log(PrefixLogMessage("Can't continue a non-existing story!"));
                EndStory();
                return;
            }

            if (!resume)
            {
                ActiveStory.Continue();
            }

            if (TextMode == ShowTextMode.Instant)
            {
                ShowStoryInstant();
            }
            else if (TextMode == ShowTextMode.WordByWord || TextMode == ShowTextMode.WordByWordlength)
            {
                ShowStoryWordByWord();
            } else if (TextMode == ShowTextMode.LetterByLetter) { 
                ShowStoryLetterByLetter();
            } else
            {
                Debug.LogError(PrefixLogMessage($"Unhandled mode {TextMode}"));
            }
        }

        public void EndStory()
        {
            Debug.Log(PrefixLogMessage("Ending story"));
            var story = ActiveStory;
            ActiveStory = null;
            trigger = null;
            NextButton.onClick.RemoveListener(EndStory);
            StoryUIRoot.SetActive(false);
            OnStoryPhaseChange?.Invoke(StoryPhase.End, story);
        }

        float nextShowTime;

        bool AnimatingStory =>
            TextMode != ShowTextMode.Instant &&
            showStoryParts != null;

        float CalculateDelay(string part)
        {
            var delay = showStorySpeed;

            if (TextMode == ShowTextMode.WordByWordlength)
            {
                delay += part.Length * showStoryCharacterSpeedCost;
            }
            if (extraCostOnPunctuaton && (punctuationCharacters.Contains(part.LastOrDefault())))
            {
                delay += showStoryPunctuationCost;
            }

            return delay;
        }

        private void Update()
        {
            if (!AnimatingStory || Time.timeSinceLevelLoad < nextShowTime) return;

            showStoryPartsIndex++;
            StoryUI.text = string.Join(
                TextMode == ShowTextMode.LetterByLetter ? "" : " ", 
                showStoryParts.Take(showStoryPartsIndex));

            if (showStoryPartsIndex >= showStoryParts.Length)
            {
                ShowProgressOptions();
                showStoryParts = null;
            } else
            {
                nextShowTime = Time.timeSinceLevelLoad + CalculateDelay(
                    showStoryParts.Take(showStoryPartsIndex).Last());
            }
        }
    }
}
