using LMCore.Crawler;
using LMCore.Inventory;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
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
            position = entity.Position;
            lookDirection = entity.LookDirection;
            anchor = entity.Anchor;
            transportationMode = entity.TransportationMode;
            rotationRespectsAnchorDirection = entity.RotationRespectsAnchorDirection;
            falling = entity.Falling;
            mapName = entity.Dungeon.MapName;
        }
    }

    [System.Serializable]
    public class PlayerCharacterSave
    {
        public string characterId;
        public List<InventorySave<StackedItemInfo>> TD1DInventories;

        public PlayerCharacterSave(
            string characterId, 
            IEnumerable<InventorySave<StackedItemInfo>> inventories
        )
        {
            this.characterId = characterId;
            TD1DInventories = inventories.ToList();
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
                .Party
                .Select(member => new PlayerCharacterSave(
                    member.CharacterId,
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

        public InventorySave(string fullId, IEnumerable<T> items) {
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
    public class LevelSave
    {
        public SerializableDictionary<string, ContainerSave<StackedItemInfo>> TD1DInventories = new ();
        public SerializableDictionary<Vector3Int, DoorSave> doors = new ();
        public SerializableDictionary<Vector3Int, SpikeTrapSave> spikes = new ();
        public List<IllusionSave> illusions = new ();
    }

    [System.Serializable]
    public class GameSave 
    {
        public GameEnvironment environment;
        public List<ItemOrigin> disposedItems = new List<ItemOrigin>();
        public SerializableDictionary<string, LevelSave> levels = new SerializableDictionary<string, LevelSave>();
        public PlayerEntitySave player;

        public GameSave() { }
    }
}
