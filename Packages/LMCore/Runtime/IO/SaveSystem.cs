using LMCore.AbstractClasses;
using LMCore.Extensions;
using System.Linq;
using UnityEngine;

namespace LMCore.IO
{
    public abstract class SaveSystem<T> : Singleton<SaveSystem<T>> where T : new()
    {
        public static T ActiveSaveData;

        [Min(1), Tooltip("Number of saves the game allows, minimum 1")]
        public int maxSaves = 3;

        [SerializeField]
        protected AbsStorageProvider<T> Provider;

        protected string PrefixLogMessage(string message) => $"SaveSystem: {message}";

        /// <summary>
        /// Merge active / last loaded stave data with current state
        /// 
        /// It should ideally not alter the active save data in place in case save fails
        /// 
        /// Subclassed version should probably first call their base versions unless
        /// subclassed directly from SaveSystem
        /// </summary>
        /// <param name="active">the active / last loaded save data</param>
        /// <returns>Merged state of last loaded and current</returns>
        abstract protected T CreateSaveState(T active);

        /// <summary>
        /// Call to save to a slot indicated by id. No check is done agains max-saves,
        /// this is the responsibility of the entity that requests a save to request.
        /// </summary>
        /// <param name="id">save slot</param>
        /// <param name="OnSave">callback for after save is done</param>
        /// <param name="OnSaveFail">callback for when save fails</param>
        public void Save(int id, System.Action OnSave, System.Action OnSaveFail)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(PrefixLogMessage("Cannot save without the application running"));
                OnSaveFail();
                return;
            }

            if (Provider != null)
            {
                var newState = CreateSaveState(ActiveSaveData);
                Provider.Save(
                    id, 
                    newState, 
                    () =>
                    {
                        ActiveSaveData = newState;
                        OnSave();
                    }, 
                    OnSaveFail);
            } else
            {
                Debug.LogError(PrefixLogMessage($"No storage provider configured, can't save to slot {id}"));
                OnSaveFail();
            }
        }

        /// <summary>
        /// Calls all interfaces that subscribe to game state having been loaded 
        /// </summary>
        /// <param name="saveData">The save data</param>
        void Load(T saveData)
        {
            ActiveSaveData = saveData;

            int n = 0;
            foreach (var normal in UnityExtensions
                .FindObjectsByInterface<IOnLoadSave>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .OrderByDescending(i => i.OnLoadPriority))
            {
                normal.OnLoad();
                n++;
            }

            Debug.Log(PrefixLogMessage($"Save loaded and {n} behaviours called to handle load"));
        }

        /// <summary>
        /// Load function
        /// </summary>
        /// <param name="id">Save lot to load</param>
        /// <param name="OnLoad">Callback for after save game has been fully loaded</param>
        /// <param name="OnLoadFail">Callback for if loading fails</param>
        public void Load(int id, System.Action OnLoad, System.Action OnLoadFail)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(PrefixLogMessage("Cannot save without the application running"));
                OnLoadFail();
                return;
            }

            if (Provider != null)
            {
                Provider.Load(
                    id, 
                    saveGame => { 
                        Load(saveGame);
                        OnLoad(); 
                    }, 
                    OnLoadFail);
            } else
            {
                Debug.LogError(PrefixLogMessage($"No storage provider configured, can't load slot {id}"));
                OnLoadFail();
            }
        }

        public void DeleteAllSaves()
        {
            foreach (var saveInfo in Provider.List())
            {
                if (!Provider.Delete(saveInfo.Id))
                {
                    Debug.LogWarning(PrefixLogMessage($"Failed to delete save {saveInfo.Id}"));
                }
            }
        }
    }
}
