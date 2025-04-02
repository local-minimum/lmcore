using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDPlayerEntity : MonoBehaviour, IOnLoadSave
    {
        public GridEntity Entity;
        public List<TDPlayerCharacter> Members;

        [SerializeField]
        TDExperienceSystem xpSystem;

        public bool Alive =>
            Members.Any(c => c.Health > 0);

        public IDungeon Dungeon => GetComponentInParent<IDungeon>();

        protected string PrefixLogMessage(string message) => $"Player Entity {name}: {message}";

        /// <summary>
        /// Needs to be after item disposal init
        /// </summary>
        public int OnLoadPriority => 5;

        public void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                Debug.LogWarning(PrefixLogMessage("There was no save"));
                return;
            }

            if (save.player == null)
            {
                Debug.LogWarning(PrefixLogMessage("There was no save for me"));
                return;
            }

            var playerSave = save.player;

            // Load entity state
            playerSave.entity.LoadOntoEntity(Entity);
            Entity.Sync();
            // TODO: Check if falling must be set some other way and if it needs to be before sync or after
            Entity.Falling = playerSave.entity.falling;

            foreach (var character in Members)
            {
                var characterSave = playerSave.characters.FirstOrDefault(ch => ch.characterId == character.CharacterId);

                if (characterSave == null)
                {
                    Debug.LogError(PrefixLogMessage($"Character '{character.CharacterId}' lacks save"));
                    continue;
                }

                character.XP = characterSave.XP;
                character.Level = characterSave.Level;

                character.LoadHealth(characterSave.Health, characterSave.MaxHealth);
                character.BaseDefence = characterSave.Defence;

                foreach (var inventory in character.GetComponentsInChildren<TD1DInventory>())
                {
                    var inventorySave = characterSave.inventories.FirstOrDefault(inv => inv.fullId == inventory.FullId);
                    if (inventorySave == null)
                    {
                        Debug.LogError(PrefixLogMessage($"Inventory '{inventory.FullId}' of '{character.CharacterId}' not present in save"));
                        continue;
                    }

                    inventory.OnLoad(inventorySave);
                }
            }
        }

        public PlayerEntitySave Save() => new PlayerEntitySave(this);

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }

        public void GainXpForEveryoneAlive(int value)
        {
            if (value <= 0)
            {
                return;
            }

            foreach (var member in Members.Where(m => m.Alive).OrderBy(m => m.XP))
            {
                xpSystem.GainXP(member, value);
            }
        }

        public bool GainXpForOne(int value, out TDPlayerCharacter character)
        {
            if (value <= 0)
            {
                character = null;
                return false;
            }
            character = Members.Where(m => m.Alive).OrderBy(m => m.XP).FirstOrDefault();
            if (character != null)
            {
                xpSystem.GainXP(character, value);
                return true;
            }

            return false;
        }

        private void OnEnable()
        {
            GameSettings.FOV.OnChange += FOV_OnChange;
            FOV_OnChange(GameSettings.FOV.Value);
        }

        private void OnDisable()
        {
            GameSettings.FOV.OnChange += FOV_OnChange;
        }

        private void FOV_OnChange(float value)
        {
            GetComponentInChildren<Camera>(true).fieldOfView = value;
        }
    }
}
