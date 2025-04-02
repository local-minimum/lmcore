using System.Collections.Generic;
using UnityEngine;

namespace LMCore.IO
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

        public override string ToString() =>
            $"<Save {Id} @ {SaveTime.ToString("s")}>";
    }

    public abstract class AbsStorageProvider<T> : MonoBehaviour where T : new()
    {
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
        /// <param name="maxSaves">Number of slots manager wants available</param>
        /// </summary>
        public abstract IEnumerable<SaveInfo> List(int maxSaves);

        /// <summary>
        /// How many saves have been made 
        /// <param name="maxSaves">Number of slots manager wants available</param>
        /// </summary>
        public abstract int Count(int maxSaves);

        /// <summary>
        /// Get the first unoccupied save slot
        /// </summary>
        /// <param name="maxSaves">Number of slots manager wants available</param>
        /// <param name="id">Save slot that is free</param>
        /// <returns>If there exists a free save slot</returns>
        public abstract bool FirstFreeSave(int maxSaves, out int id);

        /// <summary>
        /// Get the first free or the oldest save slot
        /// </summary>
        /// <param name="maxSaves">Number of slots manager wants available</param>
        /// <returns>Save slot</returns>
        public abstract int FirstFreeOrOldestSave(int maxSaves);


        /// <summary>
        /// Get the most recent save slot
        /// </summary>
        /// <param name="maxSaves">Number of slots manager wants available</param>
        /// <param name="id">The most recent save slot</param>
        /// <returns>If any saves have been made</returns>
        public abstract bool MostRecentSave(int maxSaves, out int id);

        /// <summary>
        /// Output status info to console 
        /// <param name="maxSaves">Number of slots manager wants available</param>
        /// </summary>
        public abstract void LogStatus(int maxSaves);
    }
}
