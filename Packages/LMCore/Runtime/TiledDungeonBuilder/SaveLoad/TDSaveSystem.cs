using LMCore.IO;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    public class TDSaveSystem : SaveSystem<GameSave> {

        static GameSave ActiveSave;
        
        [ContextMenu("Log Status")]
        public void LogStatus()
        {
            if (Provider == null)
            {
                Debug.LogError(PrefixLogMessage("No storage provider configured"));
            } else
            {
                Provider.LogStatus();
            }
        }

        public void Save(int id, System.Action OnSave, System.Action OnSaveFail)
        {
            // TODO: Should actually gather info and save
            if (Provider != null)
            {
                Provider.Save(id, ActiveSave, OnSave, OnSaveFail);
            } else
            {
                Debug.LogError(PrefixLogMessage($"No storage provider configured, can't save to slot {id}"));
                OnSaveFail();
            }
        }

        public void Load(int id, System.Action OnLoad, System.Action OnLoadFail)
        {
            if (Provider != null)
            {
                Provider.Load(
                    id, 
                    saveGame => { 
                        ActiveSave = saveGame;
                        // TODO: Should actually load
                        OnLoad(); 
                    }, 
                    OnLoadFail);
            } else
            {
                Debug.LogError(PrefixLogMessage($"No storage provider configured, can't load slot {id}"));
                OnLoadFail();
            }
        }
    }
}
