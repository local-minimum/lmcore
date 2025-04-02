using Ink.Runtime;
using LMCore.AbstractClasses;
using LMCore.Crawler;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LMCore.TiledDungeon.Narrative
{
    public enum StoryPhase { Start, End };

    public delegate void StoryPhaseEvent(StoryPhase phase, Story story);
    public delegate void StoryTagEvent(Story story, string tag, string value);

    /// <summary>
    /// Handles the display of a story
    /// 
    /// The following tags are supported and emitted as events by the manager
    /// 
    /// - speaker: [value|Empty]
    /// - color: [value]
    /// - block: ["start"|"end"|Empty] (StoryManager handles this one internally)
    ///   Value "start"/Empty or no value will start a block.
    ///   Value "end" will preemptively end a block, else next branching or linked part ends block
    /// </summary>
    public class StoryManager : Singleton<StoryManager, StoryManager>
    {
        public static event StoryPhaseEvent OnStoryPhaseChange;
        public static event StoryTagEvent OnStoryTag;

        static Regex speakerPattern = new Regex(@" *speaker: *(.*)", RegexOptions.IgnoreCase);
        static Regex colorPattern = new Regex(@" *color: *(.*)", RegexOptions.IgnoreCase);
        static Regex blockPattern = new Regex(@" *block: *(.*)", RegexOptions.IgnoreCase);

        private enum ShowTextMode { Instant, WordByWord, WordByWordlength, LetterByLetter };

        [SerializeField]
        ShowTextMode TextMode = ShowTextMode.Instant;

        [SerializeField]
        bool skipEmptyLines;

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

        bool AllowClicks = true;

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (entity != trigger?.InteractingEntity || !AllowClicks) return;

            if (AnimatingStory)
            {
                Debug.Log(PrefixLogMessage("Eager player don't want to see text animage in"));
                FastForwardParts();
            }
            else if (NextButton.gameObject.activeSelf)
            {
                NextButton.onClick?.Invoke();
            }
        }

        Story ActiveStory;
        /// <summary>
        /// If there is a story currently playing
        /// </summary>
        public bool Playing => ActiveStory != null;

        string PrefixLogMessage(string message) =>
            $"StoryManager: {message}";

        StoryTrigger trigger;
        private void StoryTrigger_OnPlayStory(Story story, StoryTrigger trigger, bool resume)
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
            ContinueStory(resume, false);
        }

        void SyncSimpleProgress()
        {
            if (blockPresentation && ActiveStory.canContinue)
            {
                Debug.Log(PrefixLogMessage("Automatic next line in block"));
                ContinueStory(false, true);
                return;
            }

            showStoryParts = null;

            Debug.Log(PrefixLogMessage("Hiding options area since there are none"));
            if (!ActiveStory.canContinue)
            {
                NextButton.onClick.RemoveListener(ContinueStory);
                NextButton.onClick.AddListener(EndStory);
            }

            AllowClicks = false;
            NextButton.gameObject.SetActive(true);
            StartCoroutine(DelaySelectFirst(NextButton.gameObject));
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
            blockPresentation = false;

            Debug.Log(PrefixLogMessage("Showing options"));
            OptionsRoot.gameObject.SetActive(true);
            var nOptionsBefore = Options.Count;
            StoryOption option = null;

            for (int i = 0, l = ActiveStory.currentChoices.Count; i < l; i++)
            {
                var choice = ActiveStory.currentChoices[i];
                var previousOption = option;
                if (i < nOptionsBefore)
                {

                    option = Options[i];
                }
                else
                {
                    option = Instantiate(OptionPrefab, OptionsRoot);
                    option.name = $"Story Option #{i}";

                    Options.Add(option);
                }
                option.Configure(
                    choice,
                    () =>
                    {
                        if (!AllowClicks)
                        {
                            Debug.Log(PrefixLogMessage("Refused click"));
                            return;
                        }
                        Debug.Log(PrefixLogMessage($"Your choice index is {choice.index}"));
                        ActiveStory.ChooseChoiceIndex(choice.index);
                        ContinueStory();
                    });

                option.button.name = $"Option #{i} Btn";

                // Tie together the navigation
                if (previousOption != null)
                {
                    var nav = previousOption.button.navigation;
                    nav.selectOnDown = option.button;
                    previousOption.button.navigation = nav;

                    nav = option.button.navigation;
                    nav.selectOnUp = previousOption.button;
                    option.button.navigation = nav;
                }


                if (i == 0)
                {
                    AllowClicks = false;
                    StartCoroutine(DelaySelectFirst(option.button.gameObject));
                }

                if (delayShowOptionsWith > 0)
                {
                    StartCoroutine(DelayShowOption((choice.index + 1) * delayShowOptionsWith, option));
                }
                else
                {
                    option.gameObject.SetActive(true);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(OptionsRoot);
            var group = OptionsRoot.GetComponent<VerticalLayoutGroup>();
        }

        IEnumerator<WaitForSeconds> DelaySelectFirst(GameObject target)
        {
            yield return new WaitForSeconds(0.02f);

            EventSystem.current.SetSelectedGameObject(target);

            yield return new WaitForSeconds(0.02f);

            var bg = OptionsRoot.GetComponentInChildren<ButtonGroup>();
            if (bg != null)
            {
                bg.ForceSyncSelected();
            }

            AllowClicks = true;
        }

        IEnumerator<WaitForSeconds> DelayShowOption(float delay, StoryOption option)
        {
            yield return new WaitForSeconds(delay);
            option.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(OptionsRoot);
        }

        void ShowProgressOptions()
        {
            if (ActiveStory == null)
            {
                Debug.LogWarning(PrefixLogMessage("Attempting to show story options even though there's no active story"));
                return;
            }

            if (ActiveStory.currentChoices.Count == 0)
            {
                SyncSimpleProgress();
            }
            else
            {
                SyncOptionsProgress();
                showStoryParts = null;
            }
        }

        void ShowStoryInstant(bool append)
        {
            HideAllProgressOptions();

            if (append)
            {
                StoryUI.text += ActiveStory.currentText;
            }
            else
            {
                StoryUI.text = ActiveStory.currentText;
            }

            ShowProgressOptions();
        }

        int showStoryPartsIndex;
        string[] showStoryParts;


        void ShowStoryByParts(bool append, string[] newParts)
        {
            HideAllProgressOptions();

            if (append)
            {
                showStoryParts = showStoryParts.Concat(newParts).ToArray();
            }
            else
            {
                showStoryParts = newParts;
                showStoryPartsIndex = 0;
            }

            nextShowTime = Time.timeSinceLevelLoad;
            Debug.Log(PrefixLogMessage($"{showStoryParts.Length} parts to story"));
        }

        void ShowStoryWordByWord(bool append) =>
            ShowStoryByParts(append, ActiveStory.currentText.Split(" "));

        void ShowStoryLetterByLetter(bool append) =>
            ShowStoryByParts(append, Regex.Split(ActiveStory.currentText, string.Empty));

        /// <summary>
        /// Gets the value for a tag pattern. 
        ///
        /// Note that null signifies tag does not exist while empty string means it was found
        /// but without any value
        /// </summary>
        /// <returns>Found value if any</returns>
        string GetFirstTagPatternFromActiveStory(Regex pattern)
        {
            var tag = ActiveStory.currentTags.Find(s => pattern.IsMatch(s));
            string value = null;
            if (tag != null)
            {
                value = pattern.Match(tag).Groups.Skip(1).FirstOrDefault()?.Value ?? "";
            }
            return value?.Trim();
        }

        bool blockPresentation;

        void ProcessManagedTags()
        {
            var speaker = GetFirstTagPatternFromActiveStory(speakerPattern);
            if (speaker != null) OnStoryTag?.Invoke(ActiveStory, "speaker", speaker);

            var color = GetFirstTagPatternFromActiveStory(colorPattern);
            if (color != null) OnStoryTag?.Invoke(ActiveStory, "color", color);

            var block = GetFirstTagPatternFromActiveStory(blockPattern);
            if (block != null)
            {
                switch (block.ToLower())
                {
                    case "":
                    case "start":
                        OnStoryTag?.Invoke(ActiveStory, "block", "start");
                        blockPresentation = true;
                        break;
                    case "end":
                        OnStoryTag?.Invoke(ActiveStory, "block", "end");
                        blockPresentation = false;
                        break;
                    default:
                        Debug.Log(PrefixLogMessage($"Unhandled block tag value: {block}"));
                        break;
                }
            }
        }

        void ContinueStory() => ContinueStory(false, false);
        void ContinueStory(bool resume, bool append)
        {
            if (!AllowClicks) return;

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

            // bool wasBlock = blockPresentation;

            if (skipEmptyLines)
            {
                while (string.IsNullOrEmpty(ActiveStory.currentText.Replace("\n", "").Trim()))
                {
                    ProcessManagedTags();
                    ActiveStory.Continue();
                }
            }

            ProcessManagedTags();

            /*
            if (wasBlock && !blockPresentation)
            {
                ShowProgressOptions();

                Debug.Log(PrefixLogMessage("Ending story parts due to block end"));
                return;
            }
            */

            Debug.Log(PrefixLogMessage($"Current Text:\n'{ActiveStory.currentText}'"));

            if (TextMode == ShowTextMode.Instant)
            {
                ShowStoryInstant(append);
            }
            else if (TextMode == ShowTextMode.WordByWord || TextMode == ShowTextMode.WordByWordlength)
            {
                ShowStoryWordByWord(append);
            }
            else if (TextMode == ShowTextMode.LetterByLetter)
            {
                ShowStoryLetterByLetter(append);
            }
            else
            {
                Debug.LogError(PrefixLogMessage($"Unhandled mode {TextMode}"));
            }
        }

        public void EndStory()
        {
            if (!AllowClicks) return;

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
            if (extraCostOnPunctuaton && punctuationCharacters.Contains(part.LastOrDefault()))
            {
                delay += showStoryPunctuationCost;
            }

            return delay;
        }

        private void FastForwardParts()
        {
            if (showStoryParts != null)
            {
                while (showStoryPartsIndex < showStoryParts.Length)
                {
                    ShowNextPart();
                }
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage("There is nothing to fast forward"));
            }
        }

        private void ShowNextPart()
        {
            if (showStoryPartsIndex == 0)
            {
                StoryUI.text = "";
            }

            if (showStoryParts == null || showStoryPartsIndex >= showStoryParts.Length) return;

            if (showStoryPartsIndex == 0 || (StoryUI.text.Length > 0 && StoryUI.text.Last() == '\n'))
            {
                StoryUI.text += showStoryParts[showStoryPartsIndex];
            }
            else if (TextMode == ShowTextMode.LetterByLetter)
            {
                StoryUI.text += showStoryParts[showStoryPartsIndex];
            }
            else
            {
                StoryUI.text += " " + showStoryParts[showStoryPartsIndex];
            }

            showStoryPartsIndex++;
        }

        private void Update()
        {
            if (!AnimatingStory || Time.timeSinceLevelLoad < nextShowTime) return;

            ShowNextPart();

            if (showStoryPartsIndex >= showStoryParts.Length)
            {
                ShowProgressOptions();
            }
            else
            {
                nextShowTime = Time.timeSinceLevelLoad + CalculateDelay(
                    showStoryParts.Take(showStoryPartsIndex).Last());
            }
        }
    }
}
