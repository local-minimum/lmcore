using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.Menus;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.UI;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDSavingTrigger : TDFeature 
    {
        [SerializeField]
        ParticleSystem AreaEffect;
        [SerializeField]
        int effectActivationDistance = 2;
        [SerializeField]
        string ActivatePrompt = "Rest";
        [SerializeField]
        bool TriggerRestingMenu = true;

        private void OnEnable()
        {
            if (AreaEffect != null)
            {
                if (Dungeon.Player.Coordinates.ManhattanDistance(Coordinates) <= effectActivationDistance)
                {
                    AreaEffect.Play();
                } else
                {
                    AreaEffect.Stop();
                }
            }

            GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
            GridEntity.OnInteract += GridEntity_OnInteract;
        }

        private void OnDisable()
        {
            GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
            GridEntity.OnInteract -= GridEntity_OnInteract;
        }

        bool CanInteract(GridEntity entity) =>
            entity != null &&
            entity.EntityType == GridEntityType.PlayerCharacter &&
            entity.LookDirection.Translate(entity.Coordinates) == Coordinates &&
            !AbsMenu.PausingGameplay;


        public void Save()
        {
            BasicTDSaveSystem.instance.AutoSave();
            PromptUI.instance.ShowText("Game saved", 2f);
        }

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (CanInteract(entity))
            {
                Save();
                if (TriggerRestingMenu)
                {
                    var menu = AbsMenu.GetMenu(RestingMenu.Id);
                    if (!menu.ActiveMenu)
                    {
                        menu.Show();
                        if (menu is RestingMenu)
                        {
                            var restingMenu = (RestingMenu)menu;
                            restingMenu.Configure(entity, this);
                        }
                    }
                }
            }
        }


        string lastPrompt;
        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            if (entity.EntityType != GridEntityType.PlayerCharacter) return;

            if (AreaEffect != null)
            {
                if (entity.Coordinates.ManhattanDistance(Coordinates) <= effectActivationDistance)
                {
                    if (!AreaEffect.isPlaying)
                    {
                        AreaEffect.Play();
                    }
                }
                else if (AreaEffect.isPlaying)
                {
                    AreaEffect.Stop();
                }
            }

            if (CanInteract(entity))
            {
                var bindingsUI = MovementKeybindingUI.InstanceOrResource("Keybinding");
                var keyHint = bindingsUI.GetActionHint(GamePlayAction.Interact);
                lastPrompt = $"{keyHint} {ActivatePrompt}";
                PromptUI.instance.ShowText(lastPrompt);
            }
            else if (!string.IsNullOrEmpty(lastPrompt))
            {
                PromptUI.instance.HideText(lastPrompt);
                lastPrompt = null;
            }
        }
    }
}
