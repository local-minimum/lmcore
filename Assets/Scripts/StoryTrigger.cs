using Ink.Runtime;
using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.UI;
using UnityEngine;

public delegate void PlayStoryEvent(Story story, StoryTrigger trigger);

public class StoryTrigger : TDFeature 
{
    public static event PlayStoryEvent OnPlayStory;

    [SerializeField]
    TextAsset InkJSon;
    
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
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        GridEntity.OnInteract += GridEntity_OnInteract;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        GridEntity.OnInteract -= GridEntity_OnInteract;
    }

    [SerializeField]
    string StoryInteractionVerb = "Talk";

    string lastPrompt;

    bool CanContinueStory => InkStory != null && InkStory.canContinue;

    private void GridEntity_OnInteract(GridEntity entity)
    {
        if (CanContinueStory && entity.LookDirection.Translate(entity.Coordinates) == Coordinates)
        {
            Debug.Log("Invoking story");
            HidePrompt();
            OnPlayStory?.Invoke(InkStory, this);
        }
    }

    private void GridEntity_OnPositionTransition(GridEntity entity)
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
                Debug.Log("Out of story");
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
