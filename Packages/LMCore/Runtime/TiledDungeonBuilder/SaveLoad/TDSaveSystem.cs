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
            var save = new GameSave();

            // Replace Active levels
            save.levels = new SerializableDictionary<string, LevelSave>(active?.levels);           
            var levels = save.levels;

            foreach (var dungeon in FindObjectsOfType<TiledDungeon>(true))
            {

                if (!levels.ContainsKey(dungeon.MapName))
                {
                    levels.Add(dungeon.MapName, new LevelSave());
                }

                // Update inventories
                levels[dungeon.MapName].TD1DInventories = new SerializableDictionary<string, ContainerSave<StackedItemInfo>>(
                    dungeon
                        .GetComponentsInChildren<TD1DInventory>()
                        .Select(inv => new System.Collections.Generic.KeyValuePair<string, ContainerSave<StackedItemInfo>>(
                            inv.FullId, 
                            new ContainerSave<StackedItemInfo>(inv.Save())))
                );
            }

            // Merge disposed items
            save.disposedItems = active?.disposedItems == null ? 
                ItemDisposal
                    .InstanceOrCreate()
                    .SaveState() :
                ItemDisposal
                    .InstanceOrCreate()
                    .SaveState()
                    .ToHashSet()
                    .Concat(active?.disposedItems)
                    .ToList();

            return (T)save;
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
