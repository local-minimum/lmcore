using LMCore.Inventory;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledDungeon.Narrative;
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
    /// public class MyGameSave : GameSave {
    ///     public MyGameSave() {}
    ///     public MyGameSave(GameSave gameSave) {
    ///         environment = save.environment;
    ///         disposedItems = save.disposedItems;
    ///         deadEnimies = save.deadEnimies;
    ///         levels = save.levels;
    ///         storyCollections = save.storyCollections;
    ///         player = save.player;
    ///         playerStats = save.playerStats;
    ///     }
    /// }
    /// 
    /// public class MyGameSaveSystem : TDSaveSystem<MyGameSave> {
    ///    protected override MyGameSave CreateSaveState(MyGameSave active)
    ///    {
    ///        GameSave newSave = TDSaveSystem.CreateGameSave(active);
    ///        
    ///        var newSave = new MyGameSave(gameSave);
    ///
    ///        // Gather save information here for the extension of GameSave
    ///        return newSave;
    ///    }
    ///
    ///    //This probably doesn't need to be overloaded
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
        public static GameSave CreateGameSave(GameSave active)
        {

            var save = new GameSave();

            save.environment = GameEnvironment.FromApplication();

            // Replace Active levels
            save.levels = new SerializableDictionary<string, LevelSave>(active?.levels);
            var levels = save.levels;

            foreach (var dungeon in UnityEngine.Object.FindObjectsByType<TiledDungeon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {

                if (!levels.ContainsKey(dungeon.MapName))
                {
                    levels.Add(dungeon.MapName, new LevelSave());
                }
                var levelSave = levels[dungeon.MapName];

                levelSave.enemies =
                    dungeon
                        .GetComponentsInChildren<TDEnemy>()
                        .Select(enemy => enemy.Save())
                        .ToList();

                levelSave.containers = new SerializableDictionary<string, ContainerSave<StackedItemInfo>>(
                    dungeon
                        .GetComponentsInChildren<TDContainer>()
                        .Select(container => container.Save())
                );

                levelSave.floorLoot = new ContainerSave<StackedItemInfo>(
                    TDContainer.ContainerPhase.Opened,
                    dungeon.floorContariner.NodesWithStuff.Select(n =>
                    {
                        n.Sync();
                        var inv = n.inventory as TD1DInventory;

                        return new InventorySave<StackedItemInfo>(inv.FullId, inv.Save());
                    })
                );

                levelSave.doors = new SerializableDictionary<Vector3Int, DoorSave>(
                    dungeon
                        .GetComponentsInChildren<TDDoor>()
                        .Select(door => door.Save())
                );

                levelSave.actuators = new SerializableDictionary<Vector3Int, ActuatorSave>(
                    dungeon
                        .GetComponentsInChildren<TDActuator>()
                        .Select(actuator => actuator.Save())
                );

                levelSave.spikes = new SerializableDictionary<Vector3Int, SpikeTrapSave>(
                    dungeon
                        .GetComponentsInChildren<TDSpikeTrap>()
                        .Select(spike => spike.Save())
                );

                levelSave.movingPlatforms = new SerializableDictionary<Vector3Int, MovingPlatformSave>(
                    dungeon
                        .GetComponentsInChildren<TDMovingPlatform>()
                        .Select(platform => platform.Save())
                );

                levelSave.illusions = dungeon
                    .GetComponentsInChildren<TDIllusoryCubeSide>()
                    .Select(illusion => illusion.Save())
                    .ToList();

                levelSave.stories = dungeon
                    .GetComponentsInChildren<StoryTrigger>()
                    .Select(story => story.Save())
                    .Where(story => story != null)
                    .ToList();

                levelSave.playerPositions = dungeon.GetComponentInChildren<TDPlayerPositionTracker>()?.Save().ToList();

                levelSave.saveTriggers = TDSavingTrigger.CollectSaves(dungeon).ToList();
                levelSave.customTriggers = TDCustomTrigger.CollectSaves(dungeon).ToList();
            }

            // Merge story collection saves
            if (active != null)
            {
                save.storyCollections = new SerializableDictionary<string, int>(active.storyCollections);
                foreach (var collection in StoryCollection.Collections)
                {
                    var (key, value) = collection.Save();
                    save.storyCollections[key] = value;
                }
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

            // Save latest dead enemies info
            save.deadEnimies = TDEnemyPool.instance?.Save() ?? active?.deadEnimies;

            // Save player;
            var player = FindFirstObjectByType<TDPlayerEntity>();
            if (player != null)
            {
                save.player = player.Save();
                save.playerStats = player.GetComponent<TDPlayerStats>()?.Save();
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage("Could not locate a player"));
            }

            return save;
        }

        protected override T CreateSaveState(T active)
        {
            return (T)CreateGameSave(active);
        }

        virtual public void LogStatus()
        {
            if (Provider == null)
            {
                Debug.LogError(PrefixLogMessage("No storage provider configured"));
            }
            else
            {
                Provider.LogStatus(maxSaves);
            }
        }

        protected IEnumerable<KeyValuePair<SaveInfo, GameSave>> AllSaves =>
            Provider == null ? null : Provider.List(maxSaves).Select(info =>
            {
                Provider.Load(info.Id, out var save);
                return new KeyValuePair<SaveInfo, GameSave>(info, save);
            });
    }
}
