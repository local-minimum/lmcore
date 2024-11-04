using Ink.Parsed;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum StoryPhase { Start, End };

public delegate void StoryPhaseEvent(StoryPhase phase);

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
    }

    private void OnDisable()
    {
        StoryTrigger.OnPlayStory -= StoryTrigger_OnPlayStory;
    }

    Ink.Runtime.Story ActiveStory;

    private void StoryTrigger_OnPlayStory(Ink.Runtime.Story story, StoryTrigger trigger)
    {
        if (ActiveStory != null)
        {
            Debug.LogError($"We should never have two active stories, {trigger} gave me a new while one was playing. Switching.");
        }

        OnStoryPhaseChange?.Invoke(StoryPhase.Start);
        NextButton.onClick.AddListener(ContinueStory);

        ActiveStory = story;
        StoryUIRoot.SetActive(true);
        ContinueStory();
    }

    bool CanContinueStory => ActiveStory != null && ActiveStory.canContinue;

    void SyncNextButton()
    {
        if (!ActiveStory.canContinue)
        {
            NextButton.onClick.RemoveListener(ContinueStory);
            NextButton.onClick.AddListener(EndStory);
        }

        NextButton.gameObject.SetActive(true);
    }

    void ContinueStory()
    {
        if (ActiveStory == null) return;

        ActiveStory.Continue();

        StoryUI.text = ActiveStory.currentText;

        if (ActiveStory.currentChoices.Count == 0)
        {
            OptionsRoot.gameObject.SetActive(false);
            SyncNextButton();
        } else
        {
            NextButton.gameObject.SetActive(false);
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
                    choice.text,
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
            group.spacing = 10;
        }
    }

    void EndStory()
    {
        ActiveStory = null;
        NextButton.onClick.RemoveListener(EndStory);
        StoryUIRoot.SetActive(false);
        OnStoryPhaseChange?.Invoke(StoryPhase.End);
    }
}
