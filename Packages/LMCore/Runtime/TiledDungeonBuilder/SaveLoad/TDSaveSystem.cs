using LMCore.Inventory;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    public abstract class TDSaveSystem : TDSaveSystem<GameSave, TDSaveSystem> { }

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
    public abstract class TDSaveSystem<T, TSelf> : SaveSystem<T, TSelf> where T : GameSave, new() where TSelf : TDSaveSystem<T, TSelf>
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
                var levelSave = levels[dungeon.MapName];

                // Update inventories
                levelSave.TD1DInventories = new SerializableDictionary<string, ContainerSave<StackedItemInfo>>(
                    dungeon
                        .GetComponentsInChildren<TDContainer>()
                        .Select(container => container.Save())
                );

                levelSave.doors = new SerializableDictionary<Vector3Int, DoorSave>(
                    dungeon
                        .GetComponentsInChildren<TDDoor>()
                        .Select(door => new KeyValuePair<Vector3Int, DoorSave>(door.Position, door.Save()))
                );

                levelSave.spikes = new SerializableDictionary<Vector3Int, SpikeTrapSave>(
                    dungeon
                        .GetComponentsInChildren<TDSpikeTrap>()
                        .Select(spike => new KeyValuePair<Vector3Int, SpikeTrapSave>(spike.Position, spike.Save()))
                );

                levelSave.illusions = dungeon
                    .GetComponentsInChildren<TDIllusoryCubeSide>()
                    .Select(illusion => illusion.Save())
                    .ToList();
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


            // Save player;
            save.player = FindObjectOfType<TDPlayerEntity>().Save();

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
