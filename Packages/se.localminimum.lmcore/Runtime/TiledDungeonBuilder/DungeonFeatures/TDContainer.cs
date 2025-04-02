using LMCore.Crawler;
using LMCore.Inventory;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public delegate void ContainerInteractEvent(TDContainer container);
    public delegate void ContainerLootableEvent(TDContainer container);
    public delegate void ContainerNotLootableEvenet(TDContainer container);

    public class TDContainer : TDFeature, IOnLoadSave
    {
        public static event ContainerInteractEvent OnInteract;
        public static event ContainerLootableEvent OnLootable;
        public static event ContainerNotLootableEvenet OnNotLootable;

        [Serializable]
        public enum ContainerPhase
        {
            /// <summary>
            /// Requires a key to open
            /// </summary>
            Locked,
            /// <summary>
            /// Requires interaction to open
            /// </summary>
            Closed,
            /// <summary>
            /// Requires interaction to close
            /// </summary>
            Opened,
            /// <summary>
            /// Doesn't allow interaction, just displays something
            /// </summary>
            DisplayCage
        };

        [SerializeField]
        List<Animator> animators = new List<Animator>();

        [SerializeField]
        string UnlockOpenTrigger;

        [SerializeField]
        string OpenTrigger;

        [SerializeField]
        string SyncOpenedTrigger;

        [SerializeField]
        string SyncClosedTrigger;

        [SerializeField]
        string SyncLockedTrigger;

        [SerializeField]
        string SyncDisplayCageTrigger;

        [SerializeField, HideInInspector]
        ContainerPhase phase;

        [SerializeField, HideInInspector]
        Direction facingDirection;

        [SerializeField, HideInInspector]
        Direction cubeFace;

        [SerializeField, HideInInspector]
        string key;

        [SerializeField, HideInInspector]
        bool consumesKey;

        [SerializeField, HideInInspector]
        AbsInventory inventory;
        public AbsInventory Inventory => inventory;

        [SerializeField, HideInInspector]
        bool blockingPassage;

        public bool BlockingPassage => blockingPassage;
        public string InventoryId => inventory?.FullId;

        private string PrefixLogMessage(string message) => $"Container '{name}'/'{InventoryId}' @ {Coordinates}: {message}";

        [ContextMenu("Info")]
        void Info() =>
            Debug.Log(PrefixLogMessage($"Is {phase} using key '{key}' consumes({consumesKey}) blocking passage({blockingPassage})"));

        /// <summary>
        /// Must be after the item disposal loading
        /// </summary>
        public int OnLoadPriority => 1000;

        public bool Configure(
            TDNodeConfig nodeConfig,
            Direction anchor,
            Direction facingDirection,
            string containerClass,
            TileModification[] modifications,
            bool blockingPassage
            )
        {
            var tileProps = modifications.FirstOrDefault(mod =>
                mod.Tile.Type == containerClass)?.Tile
                .CustomProperties;

            var prop = nodeConfig?.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.ObjContainerClass);

            var interaction =
                prop?.InteractionOrDefault(TiledConfiguration.instance.InteractionKey,
                prop?.InteractionOrDefault(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Open) ?? TDEnumInteraction.Open) ?? TDEnumInteraction.Open;

            var key = nodeConfig?.FirstValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props?.String(TiledConfiguration.instance.KeyKey)
            );

            var consumesKey = nodeConfig?.FirstValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.ConusumesKeyKey)
            ) ?? false;

            if (nodeConfig != null)
            {
                if (GetInventoryConfig(nodeConfig, out string id, out int capacity, out var content))
                {
                    Debug.Log(PrefixLogMessage("Container lacks inventory, so lets remove it"));
                    Configure(id, capacity, content, anchor, facingDirection, blockingPassage, interaction, key, consumesKey);
                    return true;
                }
                else
                {
                    Debug.LogError(PrefixLogMessage("There was no inventory for the container!"));
                    return false;
                }
            }
            else
            {
                Debug.LogError(PrefixLogMessage("Got no node!"));
                return false;
            }
        }

        public bool Configure(
            string id,
            int capacity,
            List<SlotContent> content,
            Direction anchor,
            Direction facingDirection,
            bool blockingPassage,
            TDEnumInteraction interaction,
            string key = null,
            bool consumesKey = false
            )
        {
            this.blockingPassage = blockingPassage;
            this.facingDirection = facingDirection;
            cubeFace = anchor;
            this.key = key;
            this.consumesKey = consumesKey;

            switch (interaction)
            {
                case TDEnumInteraction.Open:
                    phase = ContainerPhase.Opened;
                    break;
                case TDEnumInteraction.Locked:
                    phase = ContainerPhase.Locked;
                    break;
                case TDEnumInteraction.Closed:
                    phase = ContainerPhase.Closed;
                    break;
                case TDEnumInteraction.Obstruction:
                    phase = ContainerPhase.DisplayCage;
                    break;
                default:
                    Debug.LogError(PrefixLogMessage($"Recieved interaction {interaction} which it doesn't know how to do"));
                    phase = ContainerPhase.Closed;
                    break;
            }

            ConfigureInventory(id, capacity, content);

            Debug.Log(PrefixLogMessage($"Phase({phase}) Key({key}) Direction({facingDirection}) Capacity({inventory?.Capacity}) Items({inventory?.Used})"));
            return true;
        }

        [System.Serializable]

        public struct SlotContent
        {
            public int slotIndex;
            public string itemId;
            public int stackSize;

            public override string ToString() =>
                $"<Content {slotIndex}: {stackSize} x '{itemId}'>";
        }

        bool GetInventoryConfig(TDNodeConfig nodeConfig, out string id, out int capacity, out List<SlotContent> content)
        {
            var prop = nodeConfig.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.ObjContainerClass);
            if (prop != null)
            {
                id = prop.String(TiledConfiguration.instance.ObjIdKey);
                capacity = prop.Int(TiledConfiguration.instance.ObjCapacityKey, 0);

                content = new List<SlotContent>();

                for (int slotIndex = 0; slotIndex < capacity; slotIndex++)
                {
                    var itemIdKey = TiledConfiguration.instance.ObjItemPatternKey.Replace("%", slotIndex.ToString());
                    var itemId = prop.String(itemIdKey);

                    if (itemId == null) continue;

                    if (itemId == string.Empty)
                    {
                        Debug.LogWarning(PrefixLogMessage($"Item {slotIndex} configured but without id value"));
                        continue;
                    }

                    var stackSizeKey = TiledConfiguration.instance.ObjItemStackSizePatternKey.Replace("%", slotIndex.ToString());
                    var stackSize = prop.Int(stackSizeKey, 1);

                    if (stackSize > 0)
                    {
                        content.Add(new SlotContent() { slotIndex = slotIndex, itemId = itemId, stackSize = stackSize });
                    }
                }

                return true;
            }
            else
            {
                Debug.Log(PrefixLogMessage($"Inventory for {nodeConfig.Coordinates} ({name}) has no configuration, removing it"));
                DestroyImmediate(inventory);
                id = null;
                capacity = 0;
                content = null;
                return false;
            }
        }

        bool ConfigureInventory(string id, int capacity, List<SlotContent> content)
        {
            var factory = SimpleItemFactory.instance;

            inventory = GetComponentInChildren<AbsInventory>();
            if (inventory != null)
            {
                inventory.Configure(
                    id,
                    transform.parent?.GetComponentInParent<AbsInventory>(true),
                    capacity
                );

                foreach (var item in inventory.Items.ToList())
                {
                    if (inventory.Remove(item))
                    {
                        if (item != null)
                        {
                            factory.Recycle(item);
                        }
                    }
                }

                Debug.Log(PrefixLogMessage($"Inventory Capacity({inventory.Capacity})"));
                for (int i = 0; i < capacity; i++)
                {
                    if (!content.Any(c => c.slotIndex == i)) continue;

                    var instruction = content.First(c => c.slotIndex == i);

                    Debug.Log(PrefixLogMessage($"Requesting {instruction}"));

                    var items = Enumerable
                        .Range(0, instruction.stackSize)
                        .Select(_ =>
                        {
                            if (factory.Create(instruction.itemId, inventory.FullId, out var item))
                            {
                                item.transform.SetParent(transform);

                                if (item.WorldRoot != null) item.WorldRoot.gameObject.SetActive(false);
                                return item;
                            }
                            return null;
                        })
                        .Where(item => item != null)
                        .ToList();

                    if (!inventory.AddItemsToEmptyStack(instruction.slotIndex, items))
                    {
                        Debug.LogError(PrefixLogMessage($"{inventory} refused"));
                    }
                }
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage("Lacking inventory"));
                return false;
            }

            var displayInventory = GetComponent<WorldInventoryDisplay>();
            if (displayInventory != null)
            {
                displayInventory.Sync();
            }
            return true;
        }

        void Start()
        {
            InitStartCoordinates();
        }


        private void OnEnable()
        {
            GridEntity.OnInteract += GridEntity_OnInteract;
            GridEntity.OnPositionTransition += CheckShowPrompt;

            AbsMenu.OnShowMenu += HandleMenusShowing;
            AbsMenu.OnExitMenu += HandleMenusShowing;
            AbsMenu.OnHideMenus += HandleMenuseClosed;

            GameSettings.Autoloot.OnChange += Autoloot_OnChange;
            autoLoot = GameSettings.Autoloot.Value;
        }

        private void OnDisable()
        {
            GridEntity.OnInteract -= GridEntity_OnInteract;
            GridEntity.OnPositionTransition -= CheckShowPrompt;

            AbsMenu.OnShowMenu -= HandleMenusShowing;
            AbsMenu.OnExitMenu -= HandleMenusShowing;
            AbsMenu.OnHideMenus -= HandleMenuseClosed;

            GameSettings.Autoloot.OnChange -= Autoloot_OnChange;
        }

        bool autoLoot;

        private void Autoloot_OnChange(bool value)
        {
            autoLoot = value;
        }


        private void HandleMenuseClosed()
        {
            CheckShowPrompt(Dungeon.Player);
        }

        private void HandleMenusShowing(AbsMenu menu)
        {
            if (menu.ActiveMenu) HideLastPrompt();

            if (animators == null || animators.All(a => a.enabled != AbsMenu.PausingGameplay)) return;

            foreach (var animator in animators)
            {
                animator.enabled = !AbsMenu.PausingGameplay;
            }
        }

        bool isLootable;

        void EndLootability()
        {
            if (isLootable)
            {
                OnNotLootable?.Invoke(this);
            }
            isLootable = false;
        }

        void SetLootability()
        {
            isLootable = true;
            OnLootable?.Invoke(this);
        }

        private void CheckShowPrompt(GridEntity entity)
        {
            // TODO: This is a bit of a hack just assuming that non-pausing menus would be fine with 
            // having a loot prompt while all pausing menus should have it removed. But it correlates
            // at the moment.
            if (entity.EntityType != GridEntityType.PlayerCharacter || AbsMenu.PausingGameplay) return;

            if (!EntityOrientedToAllowInteraction(entity))
            {
                HideLastPrompt();
                EndLootability();
                return;
            }

            var keyHint = InputBindingsManager
                .InstanceOrResource("InputBindingsManager")
                .GetActiveActionHint(GamePlayAction.Interact);

            string newPrompt = null;
            if (phase == ContainerPhase.Locked)
            {
                if (EntityKeyHolder(entity) == null)
                {
                    newPrompt = "Requires key";
                }
                else
                {
                    newPrompt = $"{keyHint} Unlock";
                }
            }
            else if (phase == ContainerPhase.Closed)
            {
                newPrompt = $"{keyHint} Open";
            }
            else if (phase == ContainerPhase.Opened)
            {
                var items = inventory.Items.ToList();
                if (autoLoot)
                {
                    if (items.Count == 1)
                    {
                        newPrompt = $"{keyHint} Pick up {items[0].Name}";
                    }
                    else if (items.Count == 0)
                    {
                        newPrompt = $"{keyHint} Interact";
                    }
                    else
                    {
                        newPrompt = $"{keyHint} Pick up {items.Count} items";
                    }
                }
                else
                {
                    newPrompt = $"{keyHint} Loot";
                }
                SetLootability();
            }
            else
            {
                HideLastPrompt();
                EndLootability();
                return;
            }

            if (newPrompt != lastPrompt)
            {
                HideLastPrompt();
                lastPrompt = newPrompt;
                PromptUI.instance.ShowText(lastPrompt);
            }
        }

        string lastPrompt;
        void HideLastPrompt()
        {
            if (!string.IsNullOrEmpty(lastPrompt))
            {
                PromptUI.instance.RemoveText(lastPrompt);
                lastPrompt = null;
            }
        }

        /// <summary>
        /// Checks entity position and look direction and compares with the container
        /// to see if they are compatible.
        /// 
        /// Note that it doesn't answer questions like if entity can unlock the container
        /// </summary>
        /// <param name="entity">The entity that wants to interact</param>
        /// <returns>If interaction is permissable</returns>
        private bool EntityOrientedToAllowInteraction(GridEntity entity)
        {
            var coordinates = Coordinates;
            //Debug.Log(PrefixLogMessage($"Distance({entity.Coordinates.ManhattanDistance(coordinates)}) Same Elevation({entity.Coordinates.y == coordinates.y}) Entity Looks {entity.LookDirection} Anchor({cubeFace}) Facing({facingDirection})"));
            var lookingAtWithinReach = entity.LookDirection.Translate(entity.Coordinates) == coordinates;
            var entityNode = entity.Node;
            var reachable = entityNode != null && entityNode.AllowExit(entity, entity.LookDirection, false);

            // A floor chest that can be accessed from any direction
            if (cubeFace == Direction.Down && facingDirection == Direction.None)
            {
                return lookingAtWithinReach && reachable;
            }

            // A floor chest which can be accessed from one direction
            if (cubeFace == Direction.Down)
            {
                return lookingAtWithinReach &&
                    reachable &&
                    entity.LookDirection == facingDirection.Inverse();
            }

            // A wall chest that can be accessed from the same tile
            return entity.Coordinates == coordinates
                && entity.LookDirection == cubeFace;
        }

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (EntityOrientedToAllowInteraction(entity))
            {
                if (phase == ContainerPhase.Locked)
                {
                    Debug.Log(PrefixLogMessage($"{entity.name} is unlocking me"));
                    HandleUnlock(entity);
                }
                else if (phase == ContainerPhase.Closed)
                {
                    Debug.Log(PrefixLogMessage($"{entity.name} is opening me"));
                    SetTrigger(OpenTrigger);
                    phase = ContainerPhase.Opened;
                }
                else if (phase == ContainerPhase.Opened)
                {
                    HandleLoot(entity);
                }

                CheckShowPrompt(entity);
            }

        }

        /// <summary>
        /// Get the entity's inventory that holds the needed key
        /// </summary>
        /// <param name="entity">Entity to be checked for having key</param>
        /// <returns>Inventory or null</returns>
        AbsInventory EntityKeyHolder(GridEntity entity)
        {
            var keyHolder = entity
                .GetComponentsInChildren<AbsInventory>()
                .FirstOrDefault(i => i.HasItem(key));
            return keyHolder;
        }

        void SetTrigger(string trigger)
        {
            if (animators == null || string.IsNullOrEmpty(trigger)) return;
            for (int i = 0, n = animators.Count; i < n; i++)
            {
                animators[i].SetTrigger(trigger);
            }

        }

        void HandleUnlock(GridEntity entity)
        {
            var keyHolder = EntityKeyHolder(entity);
            if (keyHolder == null)
            {
                Debug.LogWarning(PrefixLogMessage($"requires key ({key})"));
            }

            if (consumesKey)
            {
                if (keyHolder.Consume(key, out string _))
                {
                    PromptUI.instance.ShowText("Lost key", 2);
                }
                else
                {
                    Debug.LogWarning(PrefixLogMessage($"Failed to consume {key} from {keyHolder}"));
                }
            }

            if (animators != null)
            {
                foreach (var animator in animators)
                {
                    animator.SetTrigger(UnlockOpenTrigger);
                }
            }
            phase = ContainerPhase.Opened;
        }

        void HandleLoot(GridEntity entity)
        {
            Debug.Log(PrefixLogMessage($"Being looted by {entity.name}, inventory has items in {inventory.Used} stacks"));
            if (autoLoot && !inventory.Empty)
            {
                var entityInventory = entity.Inventory;
                if (entityInventory == null)
                {
                    Debug.LogWarning(PrefixLogMessage($"{entity.name} doesn't have a default inventory"));
                    return;
                }

                bool allLooted = true;
                foreach (var item in inventory.Items.ToList())
                {
                    if (entityInventory.Add(item))
                    {
                        if (!inventory.Remove(item))
                        {
                            Debug.LogWarning(PrefixLogMessage($"Failed to remove {item.FullId}"));
                        }
                        else
                        {
                            Debug.Log(PrefixLogMessage($"{entity.name} looted {item.FullId}"));
                        }
                    }
                    else
                    {
                        Debug.Log(PrefixLogMessage($"Entity could not pickup {item.FullId}"));
                        allLooted = false;
                    }
                }

                if (!allLooted)
                {
                    OnInteract?.Invoke(this);
                }
            }
            else
            {
                OnInteract?.Invoke(this);
            }
        }

        public KeyValuePair<string, ContainerSave<StackedItemInfo>> Save()
        {
            var inventories = GetComponentsInChildren<TD1DInventory>()
                .Select(inv => new InventorySave<StackedItemInfo>(inv.FullId, inv.Save()));

            return new KeyValuePair<string, ContainerSave<StackedItemInfo>>(InventoryId, new ContainerSave<StackedItemInfo>(phase, inventories));
        }

        public void OnLoadGameSave(GameSave save)
        {
            if (save == null) return;

            var dungeon = GetComponentInParent<TiledDungeon>();

            var containerSave = save.levels[dungeon.MapName].containers[InventoryId];

            phase = containerSave.phase;
            switch (phase)
            {
                case ContainerPhase.Locked:
                    SetTrigger(SyncLockedTrigger);
                    break;
                case ContainerPhase.Opened:
                    SetTrigger(SyncOpenedTrigger);
                    break;
                case ContainerPhase.Closed:
                    SetTrigger(SyncClosedTrigger);
                    break;
                case ContainerPhase.DisplayCage:
                    SetTrigger(SyncDisplayCageTrigger);
                    break;
            }

            foreach (var inventory in GetComponentsInChildren<TD1DInventory>())
            {
                var inventorySave = containerSave.inventories.FirstOrDefault(s => s.fullId == inventory.FullId);
                if (inventorySave == null)
                {
                    Debug.LogWarning(PrefixLogMessage($"Inventory {inventory.FullId} doesn't exist in save"));
                    continue;
                }

                inventory.OnLoad(inventorySave);
            }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
    }
}
