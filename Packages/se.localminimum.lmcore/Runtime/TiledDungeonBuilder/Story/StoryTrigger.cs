using Ink.Runtime;
using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.UI;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.Narrative
{
    public delegate void PlayStoryEvent(Story story, StoryTrigger trigger, bool resume = false);

    public class StoryTrigger : TDFeature, IOnLoadSave
    {
        public static event PlayStoryEvent OnPlayStory;

        [SerializeField]
        string storyId = System.Guid.NewGuid().ToString();

        [ContextMenu("Generate ID")]
        void GenerateID()
        {
            storyId = System.Guid.NewGuid().ToString();
        }

        [ContextMenu("Use filename as ID")]
        void UseFileNameAsId()
        {
            if (InkJson != null)
            {
                storyId = InkJson.name;
            }
            else
            {
                storyId = null;
            }
        }

        [SerializeField]
        TextAsset InkJson;

        public enum StoryMode { OneShot, RepeatableStateless, RepeatableStatefull };
        int playCount;

        [SerializeField]
        StoryMode Mode = StoryMode.RepeatableStateless;

        [SerializeField, Tooltip("Leave empty if just restarting from the start of the story")]
        string stateFullRepeatStartPath;

        [SerializeField, Tooltip("If the trigger doesn't invok stories itself but must be triggered by 3rd party")]
        bool Passive = false;
        [SerializeField, Tooltip("If not passive, how long before we can trigger again")]
        float DelayRetriggerIfActive = 1f;
        public float DelayRegrigger => DelayRetriggerIfActive;

        Story _InkStory;
        public Story InkStory
        {
            get
            {
                if (_InkStory == null && InkJson != null)
                {
                    _InkStory = new Story(InkJson.text);
                }
                return _InkStory;
            }
        }

        public void SideLoadStory(Story story)
        {
            _InkStory = story;
        }

        private void ResetStoryTrigger()
        {
            if (InteractingEntity != null)
            {
                StoryManager.instance.EndStory();
            }
            playCount = 0;
            InteractingEntity = null;
            _InkStory = null;
        }

        private void OnEnable()
        {
            if (!Passive)
            {
                GridEntity.OnPositionTransition += CheckShowPrompt;
                GridEntity.OnInteract += SpawnStory;
            }
            StoryManager.OnStoryPhaseChange += StoryManager_OnStoryPhaseChange;
        }

        private void OnDisable()
        {
            if (!Passive)
            {
                GridEntity.OnPositionTransition -= CheckShowPrompt;
                GridEntity.OnInteract -= SpawnStory;
            }
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
                    _InkStory = null;
                }
                else if (Mode == StoryMode.RepeatableStatefull)
                {
                    if (string.IsNullOrEmpty(stateFullRepeatStartPath))
                    {
                        InkStory.state.GoToStart();
                    }
                    else
                    {
                        InkStory.ChoosePathString(stateFullRepeatStartPath, false);
                    }
                }

                if (!Passive)
                {
                    StartCoroutine(DelayReady());
                }
                else
                {
                    InteractingEntity.MovementBlockers.Remove(this);
                    InteractingEntity = null;
                }
            }
            else if (phase == StoryPhase.Start)
            {
                InteractingEntity.MovementBlockers.Add(this);
            }
        }

        IEnumerator<WaitForSeconds> DelayReady()
        {
            yield return new WaitForSeconds(DelayRetriggerIfActive);

            InteractingEntity.MovementBlockers.Remove(this);
            var entity = InteractingEntity;
            InteractingEntity = null;

            if (Mode != StoryMode.OneShot)
            {
                CheckShowPrompt(entity);
            }
        }

        public string StoryInteractionVerb = "Talk";

        string lastPrompt;
        public bool ShowingPrompt => !string.IsNullOrEmpty(lastPrompt);

        private bool HasContinuableStory =>
            InkStory != null && InkStory.canContinue;

        public bool CanContinueStory =>
            HasContinuableStory
            && (playCount == 0 || Mode != StoryMode.OneShot);

        public bool SideloadingStoryAllowed =>
            InkStory == null && (
                (playCount == 0 && Mode == StoryMode.OneShot) ||
                Mode == StoryMode.RepeatableStateless
            );

        public GridEntity InteractingEntity { get; private set; }

        private string PrefixLogMessage(string message) =>
            $"StoryTrigger {name}: {message}";

        public void SpawnStory(GridEntity entity)
        {
            if (InteractingEntity == null
                && CanContinueStory
                && (Passive || entity.LookDirection.Translate(entity.Coordinates) == Coordinates))
            {
                Debug.Log(PrefixLogMessage("Invoking story"));
                HidePrompt();
                InteractingEntity = entity;
                playCount++;
                OnPlayStory?.Invoke(InkStory, this);
            }
        }

        public void ShowPromptIfAvailable()
        {
            if (CanContinueStory)
            {
                var keyHint = InputBindingsManager
                    .InstanceOrResource("InputBindingsManager")
                    .GetActiveActionHint(GamePlayAction.Interact);

                lastPrompt = $"{keyHint} {StoryInteractionVerb}";
                PromptUI.instance.ShowText(lastPrompt);
            }
            else
            {
                Debug.Log(PrefixLogMessage("Out of story"));
            }

        }

        private void CheckShowPrompt(GridEntity entity)
        {
            if (entity.EntityType != GridEntityType.PlayerCharacter || Passive) return;

            if (entity.LookDirection.Translate(entity.Coordinates) == Coordinates
                // This basically should only happen when loading a save or if we make multiplayer
                && InteractingEntity == null)
            {
                ShowPromptIfAvailable();
            }
            else if (ShowingPrompt)
            {
                HidePrompt();
            }
        }

        public void HidePrompt()
        {
            if (ShowingPrompt)
            {
                PromptUI.instance.RemoveText(lastPrompt);
                lastPrompt = null;
            }
        }

        public StoryTriggerSave Save() => string.IsNullOrEmpty(storyId) ? null : new StoryTriggerSave()
        {
            Id = storyId,
            PlayCount = playCount,
            InteractingEntityId = InteractingEntity?.Identifier,
            StoryState = Mode == StoryMode.RepeatableStatefull ? InkStory.state.ToJson() : null,
        };


        #region Load/Save
        public int OnLoadPriority => 500;

        private void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }

            var lvl = Dungeon.MapName;
            var storySave = save.levels[lvl]?.stories?.Find(s => s.Id == storyId);

            ResetStoryTrigger();

            if (storySave == null) return;

            playCount = storySave.PlayCount;
            if (!string.IsNullOrEmpty(storySave.StoryState))
            {
                InkStory.state.LoadJson(storySave.StoryState);
            }

            if (!string.IsNullOrEmpty(storySave.InteractingEntityId))
            {
                var entity = Dungeon.GetEntity(storySave.InteractingEntityId);
                if (entity != null)
                {
                    Debug.Log(PrefixLogMessage("Invoking story from save"));
                    InteractingEntity = entity;
                    OnPlayStory?.Invoke(InkStory, this, true);
                }
                else
                {
                    Debug.LogError(PrefixLogMessage($"Could not find {storySave.InteractingEntityId} that was using me when saving"));
                }
            }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
        #endregion
    }
}
