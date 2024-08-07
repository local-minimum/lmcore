using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.IO
{
    public abstract class AbsStorageProvider<T> : MonoBehaviour where T : new()
    {
        public struct SaveInfo
        {
            public readonly int Id;
            public readonly System.DateTime SaveTime;

            public static readonly SaveInfo Nothing = new SaveInfo();

            public SaveInfo(int id, System.DateTime saveTime)
            {
                Id = id;
                SaveTime = saveTime;
            }

        }

        /// <summary>
        /// If save exist
        /// </summary>
        /// <param name="id">Save slot</param>
        public abstract bool HasSave(int id);

        /// <summary>
        /// Synchronous load
        /// </summary>
        /// <param name="id">Save slot</param>
        /// <param name="value">The saved data</param>
        /// <returns>If load was successful</returns>
        public abstract bool Load(int id, out T value);

        /// <summary>
        /// Asynchrounous load
        /// </summary>
        /// <param name="id">Save slot</param>
        /// <param name="OnLoad">Called on successful load with the loaded object</param>
        /// <param name="OnLoadFail">Called on failed load</param>
        public abstract void Load(int id, System.Action<T> OnLoad, System.Action OnLoadFail);

        /// <summary>
        /// Synchronous save
        /// </summary>
        /// <param name="id">Save slot</param>
        /// <param name="value">Object to save</param>
        /// <returns>If save was successful</returns>
        public abstract bool Save(int id, T value);

        /// <summary>
        /// Asynchronous save
        /// </summary>
        /// <param name="id">Save slot</param>
        /// <param name="value">Object to save</param>
        /// <param name="OnSaveComplete">Called after save is completed. If called with true, it was successful</param>
        public abstract void Save(int id, T value, System.Action OnSave, System.Action OnSaveFail);
        
        /// <summary>
        /// Delete a save
        /// </summary>
        /// <param name="id">Save slot</param>
        /// <returns>If delete was successful</returns>
        public abstract bool Delete(int id);

        /// <summary>
        /// Retrieve info about a save
        /// </summary>
        /// <param name="id">Save slot</param>
        /// <param name="info">Information about the save</param>
        /// <returns>If info describes a real save</returns>
        public abstract bool Info(int id, out SaveInfo info);

        /// <summary>
        /// List info about all saves that exists
        /// </summary>
        public abstract IEnumerable<SaveInfo> List();

        /// <summary>
        /// How many saves have been made 
        /// </summary>
        public abstract int Count();

        /// <summary>
        /// Get the first unoccupied save slot
        /// </summary>
        /// <param name="id">Save slot that is free</param>
        /// <returns>If there exists a free save slot</returns>
        public abstract bool FirstFreeSave(out int id);

        /// <summary>
        /// Get the first free or the oldest save slot
        /// </summary>
        /// <returns>Save slot</returns>
        public abstract int FirstFreeOrOldestSave();


        /// <summary>
        /// Get the most recent save slot
        /// </summary>
        /// <param name="id">The most recent save slot</param>
        /// <returns>If any saves have been made</returns>
        public abstract bool MostRecentSave(out int id);

        /// <summary>
        /// Output status info to console 
        /// </summary>
        public abstract void LogStatus();
    }
}
