using LMCore.Crawler;
using LMCore.EntitySM.State;
using LMCore.Inventory;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    [System.Serializable]
    public class PlayerStatsSave
    {
        public float playTime;
        public int steps;
        public int turns;
    }

    [System.Serializable]
    public class IllusionSave
    {
        public Vector3Int position;
        public Direction direction;
        public bool discovered;
    }

    [System.Serializable]
    public class SpikeTrapSave
    {
        public TDSpikeTrap.SpikePhase phase;

        public SpikeTrapSave(TDSpikeTrap.SpikePhase phase)
        {
            this.phase = phase;
        }
    }

    [System.Serializable]
    public class DoorSave
    {
        public bool isOpen;
        public bool isLocked;

        public DoorSave(bool isOpen, bool isLocked)
        {
            this.isOpen = isOpen;
            this.isLocked = isLocked;
        }
    }

    [System.Serializable]
    public class ActuatorSave
    {
        public bool lastActionWasPress;
        public bool active;
    }

    [System.Serializable]
    public class ConstrainedEntitySave
    {
        public string Identifier;
        public Vector3Int Offset;
        public Direction Anchor;
    }

    [System.Serializable]
    public class MovingPlatformSave
    {
        public Vector3Int currentCoordinates;
        public Direction moveDirection;
        /// <summary>
        /// How much before current time phase started
        /// </summary>
        public float phaseStartDelta;
        /// <summary>
        /// How much after current time phase ends
        /// </summary>
        public float nextPhaseDelay;
        public bool isToggled;
        public TDMovingPlatform.Phase phase;
        public bool alignedWithGrid;
        /// <summary>
        /// Identifiers to constraint offsets
        /// </summary>
        public List<ConstrainedEntitySave> constrainedEntitites = new();
    }

    [System.Serializable]
    public class GridEntitySave
    {
        public Vector3Int position;
        public Direction lookDirection;
        public Direction anchor;
        public TransportationMode transportationMode;
        public bool rotationRespectsAnchorDirection;
        public bool falling;
        public string mapName;

        public GridEntitySave(GridEntity entity)
        {
            position = entity.Coordinates;
            lookDirection = entity.LookDirection;
            anchor = entity.AnchorDirection;
            transportationMode = entity.TransportationMode;
            rotationRespectsAnchorDirection = entity.RotationRespectsAnchorDirection;
            falling = entity.Falling;
            mapName = entity.Dungeon.MapName;
        }

        /// <summary>
        /// Sets the save data to the entity, but doesn't actualy sync it.
        /// </summary>
        public void LoadOntoEntity(GridEntity entity)
        {
            if (entity.Dungeon.MapName != mapName)
            {
                Debug.LogError($"{entity} is attatched to dungeon map '{entity.Dungeon.MapName}', but save is for '{mapName}'");
            }

            entity.AnchorDirection = anchor;
            entity.LookDirection = lookDirection;
            entity.Coordinates = position;
            entity.TransportationMode = transportationMode;
            entity.RotationRespectsAnchorDirection = rotationRespectsAnchorDirection;
        }
    }

    [System.Serializable]
    public class PlayerCharacterSave
    {
        public string characterId;
        public List<InventorySave<StackedItemInfo>> inventories;
        public int XP;
        public int Level;
        public int Health;
        public int MaxHealth;
        public int Defence;

        public PlayerCharacterSave(
            TDPlayerCharacter character,
            IEnumerable<InventorySave<StackedItemInfo>> inventories
        )
        {
            characterId = character.CharacterId;
            XP = character.XP;
            Level = character.Level;
            Health = character.Health;
            MaxHealth = character.BaseMaxHealth;
            Defence = character.BaseDefence;

            this.inventories = inventories.ToList();
        }
    }

    [System.Serializable]
    public class PlayerEntitySave
    {
        public string levelName;
        public GridEntitySave entity;
        public List<PlayerCharacterSave> characters = new List<PlayerCharacterSave>();

        public PlayerEntitySave(TDPlayerEntity playerEntity)
        {
            levelName = playerEntity.Dungeon.MapName;
            entity = new GridEntitySave(playerEntity.Entity);
            characters = playerEntity
                .Members
                .Select(member => new PlayerCharacterSave(
                    member,
                    member
                        .GetComponentsInChildren<TD1DInventory>()
                        .Select(inv => new InventorySave<StackedItemInfo>(inv.FullId, inv.Save()))))
                .ToList();
        }
    }

    [System.Serializable]
    public class InventorySave<T>
    {
        public List<T> items;
        public string fullId;

        public InventorySave(string fullId, IEnumerable<T> items)
        {
            this.fullId = fullId;
            this.items = items.ToList();
        }
    }

    [System.Serializable]
    public class ContainerSave<T>
    {
        public TDContainer.ContainerPhase phase;
        public List<InventorySave<T>> inventories;

        public ContainerSave(
            TDContainer.ContainerPhase phase,
            IEnumerable<InventorySave<T>> inventories)
        {
            this.inventories = inventories.ToList();
            this.phase = phase;
        }
    }

    [System.Serializable]
    public class StoryTriggerSave
    {
        public string Id;
        public int PlayCount;
        public string InteractingEntityId;
        public string StoryState;
    }

    [System.Serializable]
    public class EnemyPatrollingSave
    {
        public bool active;
        public int loop;
        public int rank;
        public int direction;
    }

    [System.Serializable]
    public class EnemyGuardingSave
    {
        public List<Direction> directions;
        public Direction lookDirection;
        public float timeToNextTick;
    }

    [System.Serializable]
    public class EnemyHuntingSave
    {
        public string TargetId;
        public List<PathTranslation> PreviousPath;
    }

    [System.Serializable]
    public class EnemyDangerZoneSave
    {
        public Vector3Int Epicenter;
        public SerializableDictionary<Vector3Int, int> DecayingCoordinates;
    }

    [System.Serializable]
    public class EnemySave
    {
        public string Id;
        public bool Alive;
        public GridEntitySave entity;
        public EnemyDangerZoneSave dangerZone;

        public string activeState;
        public StateType activeStateType;
        public float activeStateActiveDuration;
        public bool mayTaxStay;

        public EnemyPatrollingSave patrolling;
        public EnemyGuardingSave guarding;
        public EnemyHuntingSave hunting;
    }

    [System.Serializable]
    public enum SaveType { LevelStart, Manual, Auto };

    [System.Serializable]
    public class SavingTriggerSave
    {
        public Vector3Int Coordinates;
        public SaveType SaveType;
        public bool Triggered;
    }

    public class CustomTriggerSave
    {
        public Vector3Int Coordinates;
        public string SaveId;
        public bool Triggered;
    }

    [System.Serializable]
    public class LevelSave
    {
        public SerializableDictionary<string, ContainerSave<StackedItemInfo>> containers = new();
        public ContainerSave<StackedItemInfo> floorLoot;
        public SerializableDictionary<Vector3Int, DoorSave> doors = new();
        public SerializableDictionary<Vector3Int, ActuatorSave> actuators = new();
        public SerializableDictionary<Vector3Int, SpikeTrapSave> spikes = new();
        public SerializableDictionary<Vector3Int, MovingPlatformSave> movingPlatforms = new();
        public List<IllusionSave> illusions = new();
        public List<StoryTriggerSave> stories = new();
        public List<Vector3Int> playerPositions = new();
        public List<EnemySave> enemies = new();
        public List<SavingTriggerSave> saveTriggers = new();
        public List<CustomTriggerSave> customTriggers = new();
    }

    [System.Serializable]
    public struct EnemyDeathData
    {
        public string MapName;
        public string ClassId;
        public string Id;
    }


    [System.Serializable]
    public class GameSave
    {
        public string saveTime;
        public DateTime SaveTime => DateTime.Parse(saveTime, System.Globalization.CultureInfo.InvariantCulture);

        public GameEnvironment environment;
        public List<ItemOrigin> disposedItems = new List<ItemOrigin>();
        public List<EnemyDeathData> deadEnimies = new List<EnemyDeathData>();
        public SerializableDictionary<string, LevelSave> levels = new SerializableDictionary<string, LevelSave>();
        public SerializableDictionary<string, int> storyCollections = new SerializableDictionary<string, int>();
        public PlayerEntitySave player;
        public PlayerStatsSave playerStats;

        public GameSave()
        {
            saveTime = DateTime.UtcNow.ToString("s");
        }
    }
}
