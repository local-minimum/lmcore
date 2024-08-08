using LMCore.Inventory;
using LMCore.IO;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    /// <summary>
    /// Basic tiled dungeon saving that can be extended for specific game needs
    /// </summary>
    /// <example>
    /// public class MyGameSave : GameSave {}
    /// 
    /// public class MyGameSaveSystem : TDSaveSystem<MyGameSave> {
    ///    protected override MyGameSave CreateSaveState(MyGameSave active)
    ///    {
    ///        MyGameSave newSave = base.CreateSaveState(active);
    ///
    ///            // Gather save information here for the extension of GameSave
    ///        return newSave;
    ///    }
    ///
    ///    protected override void Load(MyGameSave saveData)
    ///    {
    ///        base.Load(saveData);
    ///
    ///        // Apply save part of the save that extends GameSave
    ///    }
    ///
    ///    [ContextMenu("Log Status")]
    ///    override public void LogStatus()
    ///    {
    ///       base.LogStatus();
    ///    }
    /// }
    /// </example>
    /// <typeparam name="T">The extended GameSave that the game saves</typeparam>
    public abstract class TDSaveSystem<T> : SaveSystem<T> where T : GameSave, new()
    {
        protected override T CreateSaveState(T active)
        {
            // TODO: Actually gather a game save

            return (T)new GameSave()
            {
                disposedItems = ItemDisposal
                    .InstanceOrCreate()
                    .SaveState()
                    .ToHashSet()
                    .Concat(active.disposedItems)
                    .ToList(),
            };
        }

        virtual public void LogStatus()
        {
            if (Provider == null)
            {
                Debug.LogError(PrefixLogMessage("No storage provider configured"));
            } else
            {
                Provider.LogStatus();
            }
        }
    }
}
