using Ink.Runtime;
using LMCore.Crawler;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum StoryPhase { Start, End };

public delegate void StoryPhaseEvent(StoryPhase phase, Story story);

public class StoryManager : MonoBehaviour
{
    public static event StoryPhaseEvent OnStoryPhaseChange;

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

    List<StoryOption> Options = new List<StoryOption>();

    private void Awake()
    {
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

        if (NextButton.gameObject.activeSelf) { 
            NextButton.onClick?.Invoke();
        }
    }

    Ink.Runtime.Story ActiveStory;
    public bool Playing => ActiveStory != null;

    string PrefixLogMessage(string message) =>
        $"StoryManager: {message}";

    StoryTrigger trigger;
    private void StoryTrigger_OnPlayStory(Ink.Runtime.Story story, StoryTrigger trigger)
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
        ContinueStory();
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
        OptionsRoot.gameObject.SetActive(false);
    }

    void SyncOptionsProgress()
    {
        Debug.Log(PrefixLogMessage("Showing options"));
        NextButton.gameObject.SetActive(false);
        OptionsRoot.gameObject.SetActive(true);
        var nOptionsBefore = Options.Count;
        for (int i = 0, l = ActiveStory.currentChoices.Count; i < l; i++)
        {
            var choice = ActiveStory.currentChoices[i];
            StoryOption option = null;
            if (i < nOptionsBefore)
            {
                option = Options[i];
            } else
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
            option.gameObject.SetActive(true);
        }
        for (int i = ActiveStory.currentChoices.Count, l = Options.Count; i < l; i++)
        {
            Options[i].gameObject.SetActive(false);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(OptionsRoot);
        var group = OptionsRoot.GetComponent<VerticalLayoutGroup>();
    }

    void ContinueStory()
    {
        if (ActiveStory == null)
        {
            Debug.Log(PrefixLogMessage("Can't continue a non-existing story!"));
            EndStory();
            return;
        }

        ActiveStory.Continue();

        StoryUI.text = ActiveStory.currentText;

        if (ActiveStory.currentChoices.Count == 0)
        {
            SyncSimpleProgress();
        } else
        {
            SyncOptionsProgress();
        }
    }

    void EndStory()
    {
        Debug.Log(PrefixLogMessage("Ending story"));
        var story = ActiveStory;
        ActiveStory = null;
        trigger = null;
        NextButton.onClick.RemoveListener(EndStory);
        StoryUIRoot.SetActive(false);
        OnStoryPhaseChange?.Invoke(StoryPhase.End, story);
    }
}
