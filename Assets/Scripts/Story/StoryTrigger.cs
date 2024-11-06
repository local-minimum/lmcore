using Ink.Runtime;
using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.UI;
using System.Collections;
using UnityEngine;

public delegate void PlayStoryEvent(Story story, StoryTrigger trigger);

public class StoryTrigger : TDFeature 
{
    public static event PlayStoryEvent OnPlayStory;

    [SerializeField]
    TextAsset InkJSon;

    public enum StoryMode { OneShot, RepeatableStateless, RepeatableStatefull };
    int playCount;

    [SerializeField]
    StoryMode Mode = StoryMode.RepeatableStateless;
    [SerializeField, Tooltip("Leave empty if just restarting from the start of the story")]
    string stateFullRepeatStartPath = "Start";

    Story _InkStory;
    Story InkStory
    {
        get
        {
            if (_InkStory == null && InkJSon != null)
            {
                _InkStory = new Story(InkJSon.text);
            }
            return _InkStory;
        }
        set
        {
            _InkStory = value;
        }
    }

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += CheckCanSpawnStory;
        GridEntity.OnInteract += SpawnStory;
        StoryManager.OnStoryPhaseChange += StoryManager_OnStoryPhaseChange;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= CheckCanSpawnStory;
        GridEntity.OnInteract -= SpawnStory;
        StoryManager.OnStoryPhaseChange -= StoryManager_OnStoryPhaseChange;
    }

    private void StoryManager_OnStoryPhaseChange(StoryPhase phase, Story story)
    {
        if (story != InkStory)
        {
            if (InteractingEntity != null)
            {
                InteractingEntity.MovementBlockers.Remove(this);
                InteractingEntity = null;
            }
            return;
        }

        if (phase == StoryPhase.End)
        {
            if (Mode == StoryMode.RepeatableStateless)
            {
                InkStory = null;
            } else if (Mode == StoryMode.RepeatableStatefull)
            {
                if (string.IsNullOrEmpty(stateFullRepeatStartPath))
                {
                    InkStory.state.GoToStart();
                } else
                {
                    InkStory.ChoosePathString(stateFullRepeatStartPath, false);
                }
            }
            StartCoroutine(DelayReady());
        } else if (phase == StoryPhase.Start)
        {
            InteractingEntity.MovementBlockers.Add(this);
        }
    }

    IEnumerator DelayReady()
    {
        yield return new WaitForEndOfFrame();

        InteractingEntity.MovementBlockers.Remove(this);
        var entity = InteractingEntity;
        InteractingEntity = null;

        if (Mode != StoryMode.OneShot)
        {
            CheckCanSpawnStory(entity);
        }
    }

    [SerializeField]
    string StoryInteractionVerb = "Talk";

    string lastPrompt;

    bool CanContinueStory => InkStory != null 
        && InkStory.canContinue
        && (playCount == 0 || Mode != StoryMode.OneShot);

    public GridEntity InteractingEntity { get; private set; }

    private string PrefixLogMessage(string message) =>
        $"StoryTrigger {name}: {message}";

    private void SpawnStory(GridEntity entity)
    {
        if (InteractingEntity == null && CanContinueStory && entity.LookDirection.Translate(entity.Coordinates) == Coordinates)
        {
            Debug.Log(PrefixLogMessage("Invoking story"));
            HidePrompt();
            InteractingEntity = entity;
            playCount++;
            OnPlayStory?.Invoke(InkStory, this);
        }
    }

    private void CheckCanSpawnStory(GridEntity entity)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter) return;

        if (entity.LookDirection.Translate(entity.Coordinates) == Coordinates)
        {
            if (CanContinueStory) {
                var bindingsUI = MovementKeybindingUI.InstanceOrResource("Keybinding");
                var keyHint = bindingsUI.GetActionHint(GamePlayAction.Interact);

                lastPrompt = $"{keyHint} {StoryInteractionVerb}";
                PromptUI.instance.ShowText(lastPrompt);
            } else
            {
                Debug.Log(PrefixLogMessage("Out of story"));
            }
        }
        else if (!string.IsNullOrEmpty(lastPrompt))
        {
            HidePrompt();
        }
    }

    private void HidePrompt()
    {
        if (!string.IsNullOrEmpty(lastPrompt))
        {
            PromptUI.instance.HideText(lastPrompt);
            lastPrompt = null;
        }
    }

    /* Save state
     * ----------
     * InteractingEntity (if not null we need to be playing!)
     * Story.state.toJson
     * playCount
     * 
     */
}
