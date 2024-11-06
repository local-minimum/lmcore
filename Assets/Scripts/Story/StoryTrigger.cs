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

    [SerializeField]
    StoryMode Mode = StoryMode.RepeatableStateless;
    
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
                story.state.GoToStart();
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

    bool CanContinueStory => InkStory != null && InkStory.canContinue;

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
}
