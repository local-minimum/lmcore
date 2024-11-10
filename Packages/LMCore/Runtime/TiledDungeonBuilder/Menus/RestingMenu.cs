using Ink.Runtime;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Narrative;
using LMCore.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.TiledDungeon.Menus
{
    public class RestingMenu : AbsMenu
    {
        [SerializeField]
        GameObject MenuRoot;

        [SerializeField]
        Button ResumeButton;

        [SerializeField]
        Button EatButton;

        [SerializeField]
        Button StoryButton;

        [SerializeField]
        Button QuitButton;

        public override bool PausesGameplay =>
            true;

        protected override string MenuId => Id;
        public static string Id => "RestingMenu";

        protected override void Blur()
        {
            MenuRoot.transform.HideAllChildren();
            ResumeButton.interactable = false;
            QuitButton.interactable = false;
            StoryButton.interactable = false;
            EatButton.interactable = false;
        }

        protected override void Focus()
        {
            MenuRoot.transform.ShowAllChildren();
            Configure(player, trigger);
        }

        private void Start()
        {
            Blur();
        }

        GridEntity player;
        TDSavingTrigger trigger;

        public void Configure(GridEntity player, TDSavingTrigger trigger)
        {
            this.player = player;
            this.trigger = trigger;

            ResumeButton.interactable = true;
            QuitButton.interactable = true;

            // TODO: Check if needing food for health or status
            EatButton.interactable = false;

            storyTrigger = trigger?.GetComponent<StoryTrigger>();
            StoryButton.interactable = storyTrigger != null && storyTrigger.CanContinueStory;

#if PLATFORM_WEBGL
            QuitButton.gameObject.SetActive(false);
#endif
        }

        StoryTrigger storyTrigger;

        public void Story()
        {
            if (storyTrigger != null)
            {
                Blur();
                StoryManager.OnStoryPhaseChange += StoryManager_OnStoryPhaseChange;
                storyTrigger.SpawnStory(player);
            }
        }

        Story CurrentStory;
        private void StoryManager_OnStoryPhaseChange(StoryPhase phase, Ink.Runtime.Story story)
        {
            if (phase == StoryPhase.Start)
            {
                if (CurrentStory == null)
                {
                    CurrentStory = story;
                }
            }
            else if (phase == StoryPhase.End)
            {
                if (story == CurrentStory)
                {
                    CurrentStory = null;
                    StoryManager.OnStoryPhaseChange -= StoryManager_OnStoryPhaseChange;
                    Focus();
                }
            }
        }

        public void Eat()
        {
            // TODO: Probe inventories for edibles and consume them wisely over time
            // might be own so this one is disabled
            EatButton.interactable = false;
        }

        public void ExitApp()
        {
            Application.Quit();
        }
    }
}
