using Ink.Runtime;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.Inventory;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Narrative;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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

        /// <summary>
        /// Only really works with oneshot story triggers or stateless
        /// because we can't save the story state on the trigger
        /// </summary>
        [SerializeField, Tooltip("Use to override the stories of story triggers on the menu trigger")]
        StoryCollection Stories;

        public override bool PausesGameplay =>
            true;

        public override string MenuId => Id;
        public static string Id => "RestingMenu";

        protected override void Blur()
        {
            MenuRoot.transform.HideAllChildren();
            ResumeButton.interactable = false;
            QuitButton.interactable = false;
            StoryButton.interactable = false;
            if (EatButton != null) EatButton.interactable = false;
        }

        IEnumerable<Button> ActiveButtons
        {
            get
            {
                if (ResumeButton.gameObject.activeSelf && ResumeButton.enabled && ResumeButton.interactable)
                {
                    yield return ResumeButton;
                }
                if (EatButton != null && EatButton.gameObject.activeSelf && EatButton.enabled && EatButton.interactable)
                {
                    yield return EatButton;
                }
                if (StoryButton.gameObject.activeSelf && StoryButton.enabled && StoryButton.interactable)
                {
                    yield return StoryButton;
                }
                if (QuitButton.gameObject.activeSelf && QuitButton.enabled && QuitButton.interactable)
                {
                    yield return QuitButton;
                }

            }
        }

        protected override void Focus()
        {
            MenuRoot.transform.ShowAllChildren();
            if (player != null) Configure(player, trigger);
        }

        private void Start()
        {
            Blur();
        }

        GridEntity player;
        TDSavingTrigger trigger;

        List<TDPlayerCharacter> charactersThatCanEat = new List<TDPlayerCharacter>();

        public void Configure(GridEntity player, TDSavingTrigger trigger)
        {
            this.player = player;
            this.trigger = trigger;

            ResumeButton.interactable = true;
            QuitButton.interactable = true;

            var playerEntity = player?.GetComponentInChildren<TDPlayerEntity>();
            if (playerEntity != null)
            {

                charactersThatCanEat = playerEntity.Members
                        .Where(member =>
                            !member.FullHealth
                            && member.BackpackInventory.Items.Any(item => EdibleItem(item)))
                        .ToList();

                Debug.Log(PrefixLogMessage(
                    $"{charactersThatCanEat.Count}/{playerEntity.Members.Count} characters are hungry and can eat." +
                    $" {playerEntity.Members.Count(p => !p.FullHealth)} need to eat." +
                    $" {playerEntity.Members.Count(p => p.BackpackInventory.Items.Any(i => EdibleItem(i)))} has edibles in backpack"));

            }
            else
            {
                Debug.LogError(PrefixLogMessage($"There's no player entity on {player?.name}!"));
                charactersThatCanEat.Clear();
            }

            if (EatButton != null) EatButton.interactable = charactersThatCanEat.Count > 0;

            storyTrigger = trigger?.GetComponent<StoryTrigger>();
            if (storyTrigger != null && Stories != null)
            {
                if (storyTrigger.SideloadingStoryAllowed && Stories.ClaimStory(out var story))
                {
                    storyTrigger.SideLoadStory(story);

                    StoryButton.interactable = storyTrigger.CanContinueStory;
                }
                else
                {
                    StoryButton.interactable = storyTrigger != null && storyTrigger.CanContinueStory;
                }
            }
            else
            {
                StoryButton.interactable = storyTrigger != null && storyTrigger.CanContinueStory;
            }

#if PLATFORM_WEBGL
            QuitButton.gameObject.SetActive(false);
#endif

            SetupMenuNavigation();
        }

        bool AllowClicks = false;

        private void SetupMenuNavigation()
        {
            AllowClicks = false;
            var activeButtons = ActiveButtons.ToList();

            for (int i = 0, n = activeButtons.Count; i < n; i++)
            {
                var btn = activeButtons[i];
                var navigations = new Navigation();
                navigations.mode = Navigation.Mode.Explicit;
                if (i < n - 1)
                {
                    navigations.selectOnDown = activeButtons[i + 1];
                }
                ;

                if (i > 0)
                {
                    navigations.selectOnUp = activeButtons[i - 1];
                }
                btn.navigation = navigations;
            }

            if (activeButtons.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(activeButtons[0]?.gameObject);
            }
        }

        private void Update()
        {
            if (!AllowClicks)
            {
                AllowClicks = true;
            }
        }

        StoryTrigger storyTrigger;

        public void Story()
        {
            if (storyTrigger != null && AllowClicks)
            {
                Blur();
                StoryManager.OnStoryPhaseChange += StoryManager_OnStoryPhaseChange;
                storyTrigger.SpawnStory(player);
            }
        }

        Story CurrentStory;
        private void StoryManager_OnStoryPhaseChange(StoryPhase phase, Story story)
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

        // TODO: This is a bit wild, it would be nice to have some order to what is being eaten
        bool EdibleItem(AbsItem item) => item.Type.HasFlag(ItemType.Consumable);

        public void Eat()
        {
            if (!AllowClicks) return;

            foreach (var character in charactersThatCanEat)
            {
                int neededHealth = character.HealableAmount;
                Debug.Log(PrefixLogMessage($"{character.Name} needs {neededHealth}"));

                List<Recipe> OutlawRecipes = new List<Recipe>();

                while (neededHealth > 0)
                {
                    var consumables = character.BackpackInventory.Items.Where(item => item.Type.HasFlag(ItemType.Consumable));
                    var tools = character.BackpackInventory.Items.Where(item => item.Type.HasFlag(ItemType.Tool));
                    var recipe = RecipeCollection.instance.GetRecipesFor(consumables, tools)
                        .Where(r => !OutlawRecipes.Contains(r))
                        .OrderBy(r =>
                        {
                            var healing = r.TotalHealing;
                            if (healing <= neededHealth)
                            {
                                return healing;
                            }
                            else
                            {
                                return neededHealth - healing;
                            }
                        })
                        .FirstOrDefault();

                    bool cookFail = false;

                    if (recipe == null)
                    {
                        Debug.LogWarning(PrefixLogMessage($"{character.Name} has no recipe to cook"));
                        break;
                    }
                    else
                    {
                        Debug.Log(PrefixLogMessage($"{character.Name} attempts to make recipe: {recipe.Name}"));

                        foreach (var ingredient in recipe.Ingredients)
                        {
                            for (var i = 0; i < ingredient.Amount; i++)
                            {
                                if (character.BackpackInventory.Consume(ingredient.Id, out string origin))
                                {
                                    ItemDisposal.instance.Dispose(ingredient.Id, origin);
                                }
                                else
                                {
                                    Debug.LogError(PrefixLogMessage(
                                        $"Failed to produce recipe {recipe}, some ingredients may have been wasted."));
                                    cookFail = true;
                                    break;
                                }
                            }

                            if (cookFail) break;
                        }

                        foreach (var tool in recipe.Tools)
                        {
                            if (cookFail) break;
                            if (!tool.Consumes) continue;

                            for (var i = 0; i < tool.Amount; i++)
                            {
                                if (character.BackpackInventory.Consume(tool.Id, out string origin))
                                {
                                    ItemDisposal.instance.Dispose(tool.Id, origin);
                                }
                                else
                                {
                                    Debug.LogError(PrefixLogMessage(
                                        $"Failed to produce recipe {recipe}, some ingredients may have been wasted."));
                                    cookFail = true;
                                    break;
                                }
                            }
                        }

                        if (!cookFail)
                        {
                            TDDamageSystem.instance.HealCharacter(character, recipe.TotalHealing);
                            neededHealth -= recipe.TotalHealing;
                            Debug.Log(PrefixLogMessage(recipe.Humanize(character.Name)));
                        }
                        else
                        {
                            OutlawRecipes.Add(recipe);
                        }
                    }
                }
            }

            charactersThatCanEat.Clear();
            if (EatButton != null) EatButton.interactable = false;
        }

        public void Resume()
        {
            if (!AllowClicks) return;

            Debug.Log(PrefixLogMessage("Resume Game"));
            Exit();

            // If player has eaten and such they should not loose that progress
            // This adds Game Saved prompt on the UI
            trigger?.Save();

            // As soon as that prompt is gone, we want to show that they can rest again
            trigger?.TriggerPrompt(true);

            trigger = null;
            player = null;
        }

        public void ExitApp()
        {
            Debug.LogWarning(PrefixLogMessage("Resume Game"));
            Application.Quit();
        }
    }
}
