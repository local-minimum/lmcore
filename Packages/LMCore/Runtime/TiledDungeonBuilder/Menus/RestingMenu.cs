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

        List<TDPlayerCharacter> charactersThatCanEat = new List<TDPlayerCharacter>();
        
        public void Configure(GridEntity player, TDSavingTrigger trigger)
        {
            this.player = player;
            this.trigger = trigger;

            ResumeButton.interactable = true;
            QuitButton.interactable = true;

            var playerEntity = player?.GetComponent<TDPlayerEntity>();
            if (playerEntity != null)
            {

                charactersThatCanEat = playerEntity.Party
                        .Where(member => 
                            !member.FullHealth
                            && member.MainInventory.Items.Any(item => EdibleItem(item)))
                        .ToList();

            } else
            {
                charactersThatCanEat.Clear();
            }

            EatButton.interactable = charactersThatCanEat.Count > 0;

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
        // TODO: We need to also figure out what effect eating this has
        bool EdibleItem(AbsItem item) => item.Type.HasFlag(ItemType.Consumable);

        public void Eat()
        {
            foreach (var character in charactersThatCanEat)
            {
                // TODO: Probe inventories for edibles and consume them wisely over time
                // might be own so this one is disabled
                int neededHealth = character.HealableAmount;

                List<Recipe> OutlawRecipes = new List<Recipe>();

                while (neededHealth > 0)
                {
                    var consumables = character.MainInventory.Items.Where(item => item.Type.HasFlag(ItemType.Consumable));
                    var tools = character.MainInventory.Items.Where(item => item.Type.HasFlag(ItemType.Tool));
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

                    if (recipe != null)
                    {
                        foreach (var ingredient in recipe.Ingredients)
                        {
                            for (var i=0; i<ingredient.Amount; i++)
                            {
                                if (character.MainInventory.Consume(ingredient.Id, out string origin))
                                {
                                    ItemDisposal.instance.Dispose(ingredient.Id, origin);
                                } else
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

                            for (var i=0; i<tool.Amount; i++)
                            {
                                if (character.MainInventory.Consume(tool.Id, out string origin))
                                {
                                    ItemDisposal.instance.Dispose(tool.Id, origin);
                                } else
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
                            character.Heal(recipe.TotalHealing);
                            neededHealth -= recipe.TotalHealing;
                            Debug.Log(PrefixLogMessage(recipe.Humanize(character.Name)));
                        } else
                        {
                            OutlawRecipes.Add(recipe);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            charactersThatCanEat.Clear();
            EatButton.interactable = false;
        }

        public void ExitApp()
        {
            Application.Quit();
        }
    }
}
