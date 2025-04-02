using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.Menus;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public delegate void BeforeSaveEvent(SaveType saveType);
    public delegate void AutoSaveEvent(SaveType saveType);

    public class TDSavingTrigger : TDFeature, IOnLoadSave
    {
        static List<TDSavingTrigger> triggers = new List<TDSavingTrigger>();

        public static event BeforeSaveEvent OnBeforeSave;
        public static event AutoSaveEvent OnAutoSave;

        [SerializeField]
        SaveType saveType = SaveType.Auto;

        [SerializeField]
        ParticleSystem AreaEffect;

        [SerializeField]
        int effectActivationDistance = 2;

        [SerializeField]
        string ActivatePrompt = "Rest";

        [SerializeField]
        bool TriggerRestingMenu = true;

        [SerializeField]
        bool Repeatable = true;

        [HideInInspector]
        public bool Managed;

        #region SaveState
        bool triggered;
        #endregion

        private void OnEnable()
        {
            if (AreaEffect != null && Dungeon != null && Dungeon.Player != null)
            {
                if (Dungeon.Player.Coordinates.ManhattanDistance(Coordinates) <= effectActivationDistance)
                {
                    AreaEffect.Play();
                }
                else
                {
                    AreaEffect.Stop();
                }
            }

            GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
            GridEntity.OnInteract += GridEntity_OnInteract;
            triggers.Add(this);
        }

        private void OnDisable()
        {
            GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
            GridEntity.OnInteract -= GridEntity_OnInteract;
            triggers.Remove(this);
        }

        private void OnDestroy()
        {
            triggers.Remove(this);
        }

        bool CanInteract(GridEntity entity) =>
            entity != null &&
            entity.EntityType == GridEntityType.PlayerCharacter &&
            entity.LookDirection.Translate(entity.Coordinates) == Coordinates &&
            !AbsMenu.PausingGameplay &&
            (!triggered || Repeatable);


        public void SaveGame()
        {
            OnBeforeSave?.Invoke(saveType);
            OnAutoSave?.Invoke(saveType);
            PromptUI.instance.ShowText("Game saved", 2f);
            triggered = true;
        }

        public void InvokeTrigger(GridEntity entity)
        {
            SaveGame();
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
                    HidePrompt();
                }
            }
        }

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (!Managed && CanInteract(entity))
            {
                InvokeTrigger(entity);
            }
        }


        string lastPrompt;

        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            if (entity.EntityType != GridEntityType.PlayerCharacter || Managed) return;

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
                TriggerPrompt();
            }
            else
            {
                HidePrompt();
            }
        }

        public void HidePrompt()
        {
            if (!string.IsNullOrEmpty(lastPrompt))
            {
                PromptUI.instance.RemoveText(lastPrompt);
                lastPrompt = null;
            }
        }

        /// <summary>
        /// Triggers a prompt to be shown
        /// </summary>
        /// <param name="queue">If true, prompt is queued if something else is showing, else it overrides existing text</param>
        public void TriggerPrompt(bool queue = false)
        {
            var keyHint = InputBindingsManager
                .InstanceOrResource("InputBindingsManager")
                .GetActiveActionHint(GamePlayAction.Interact);

            lastPrompt = $"{keyHint} {ActivatePrompt}";
            if (queue)
            {
                PromptUI.instance.QueueText(lastPrompt);
            }
            else
            {
                PromptUI.instance.ShowText(lastPrompt);
            }
        }


        #region Save/Load
        public SavingTriggerSave Save()
            => triggered ?
            new SavingTriggerSave()
            {
                Coordinates = Coordinates,
                SaveType = saveType,
                Triggered = true,
            } : null;

        public static IEnumerable<SavingTriggerSave> CollectSaves(TiledDungeon dungeon) =>
            triggers
                .Where(t => t.Dungeon == dungeon)
                .Select(t => t.Save())
                .Where(s => s != null);

        // As with most world things must be after moving platforms
        // which are 10
        public int OnLoadPriority => 9;

        public void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }

            var lvl = Dungeon.MapName;

            var lvlSave = save.levels[lvl];
            if (lvlSave == null)
            {
                return;
            }

            var mySave = lvlSave.saveTriggers.FirstOrDefault(tSave => tSave.Coordinates == Coordinates && tSave.SaveType == saveType);
            if (mySave == null)
            {
                triggered = false;
            }
            else
            {
                triggered = mySave.Triggered;
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
