using LMCore.Crawler;
using LMCore.Extensions;
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
        public List<TDPlayerCharacter> Party;

        protected string PrefixLogMessage(string message) => $"Player Entity {name}: {message}";

        /// <summary>
        /// Needs to be after item disposal init
        /// </summary>
        public int OnLoadPriority => 1001;

        public void OnLoad()
        {
            var save = SaveSystem<GameSave>.ActiveSaveData;

            if (save == null || save.player == null) {
                Debug.LogWarning(PrefixLogMessage("There was no save for me"));
                return;
            }

            var playerSave = save.player;

            // Load entity state
            Entity.Dungeon = UnityExtensions.FindObjectByInterfaceOrDefault<IDungeon>(dung => dung.MapName == playerSave.entity.mapName);
            Entity.Anchor = playerSave.entity.anchor;
            Entity.LookDirection = playerSave.entity.lookDirection;
            Entity.Position = playerSave.entity.position;
            Entity.TransportationMode = playerSave.entity.transportationMode;
            Entity.RotationRespectsAnchorDirection = playerSave.entity.rotationRespectsAnchorDirection;

            Entity.Sync();

            // TODO: Check if falling must be set some other way and if it needs to be before sync or after
            Entity.Falling = playerSave.entity.falling;

            foreach (var character in Party)
            {
                var characterSave = playerSave.characters.FirstOrDefault(ch => ch.characterId == character.CharacterId);

                if (characterSave == null) {
                    Debug.LogError(PrefixLogMessage($"Character '{character.CharacterId}' lacks save"));
                    continue;
                }

                foreach (var inventory in character.GetComponentsInChildren<TD1DInventory>())
                {
                    var inventorySave = characterSave.TD1DInventories.FirstOrDefault(inv => inv.fullId == inventory.FullId);
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
    }
}
