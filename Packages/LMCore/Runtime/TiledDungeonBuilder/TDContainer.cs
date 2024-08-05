using System.Linq;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Inventory;
using LMCore.TiledDungeon.Integration;
using System.Collections.Generic;
using LMCore.Extensions;

namespace LMCore.TiledDungeon
{
    public class TDContainer : MonoBehaviour
    {       
        enum ContainerPhase { 
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
        Animator animator;

        [SerializeField]
        string UnlockOpenTrigger;

        [SerializeField]
        string OpenTrigger;

        [SerializeField, HideInInspector]
        Vector3Int Position;

        [SerializeField, HideInInspector]
        ContainerPhase phase;

        [SerializeField, HideInInspector]
        Direction direction;

        [SerializeField, HideInInspector]
        string key;

        [SerializeField, HideInInspector]
        bool consumesKey;

        [SerializeField, HideInInspector]
        AbsInventory inventory;

        [SerializeField, HideInInspector]
        bool blockingPassage;

        public bool BlockingPassage => blockingPassage;

        public void Configure(
            TDNodeConfig nodeConfig,
            Vector3Int position,
            Direction direction,
            string containerClass,
            TileModification[] modifications,
            bool blockingPassage
            )
        {
            this.blockingPassage = blockingPassage;
            Position = position;

            var tileProps = modifications.FirstOrDefault(mod =>
                mod.Tile.Type == containerClass)?.Tile
                .CustomProperties;

            var prop = nodeConfig?.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.ObjContainerClass);

            var interaction = 
                prop?.InteractionOrDefault(TiledConfiguration.instance.InteractionKey, 
                prop?.InteractionOrDefault(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Open) ?? TDEnumInteraction.Open) ?? TDEnumInteraction.Open;

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
                    Debug.LogError($"Container @ {Position}: Recieved interaction {interaction} which it doesn't know how to do");
                    phase = ContainerPhase.Closed;
                    break;
            }

            this.direction = direction;

            key = nodeConfig?.FirstObjectValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props?.String(TiledConfiguration.instance.KeyKey)
            );

            consumesKey = nodeConfig?.FirstObjectValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.ConusumesKeyKey)
            ) ?? false;

            if (nodeConfig != null)
            {
                ConfigureInventory(nodeConfig);
            } else
            {
                Debug.LogError($"Container @ {Position}: Got no node!");
            }

            Debug.Log($"Container @ {Position}: Phase({phase}) Key({key}) Direction({direction}) Capacity({inventory?.Capacity}) Items({inventory?.Used})");
        }

        void ConfigureInventory(TDNodeConfig nodeConfig)
        {
            var factory = SimpleItemFactory.instance;

            // TODO: Any reason to support multiple containers in one location?
            inventory = GetComponentInChildren<AbsInventory>();
            if (inventory != null)
            {
                var prop = nodeConfig.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.ObjContainerClass);
                if (prop != null)
                {
                    var capacity = prop.Int(TiledConfiguration.instance.ObjCapacityKey, 0);

                    inventory.Configure(
                        prop.String(TiledConfiguration.instance.ObjContainerIdKey),
                        transform.parent?.GetComponentInParent<AbsInventory>(true),
                        capacity
                    );

                    Debug.Log($"Container @ {Position}: Inventory Capacity({inventory.Capacity})");
                    for (int i  = 0; i < capacity; i++)
                    {
                        var itemIdKey = TiledConfiguration.instance.ObjItemPatternKey.Replace("%", i.ToString());
                        var itemId = prop.String(itemIdKey);

                        if (itemId == null) continue;

                        if (itemId == string.Empty)
                        {
                            Debug.LogWarning($"Container {name} @ {Position}: Item {i} configured but without id value");
                            continue;
                        }

                        var stackSizeKey = TiledConfiguration.instance.ObjItemStackSizePatternKey.Replace("%", i.ToString());
                        var stackSize = prop.Int(stackSizeKey, 1);

                        for (int n = 0; n < stackSize; n++)
                        {
                            if (factory.Create(itemId, inventory.FullId, out AbsItem item))
                            {
                                inventory.Add(item);
                                item.transform.SetParent(transform);

                                if (item.UIRoot != null) item.UIRoot.gameObject.SetActive(false);
                                if (item.WorldRoot != null) item.WorldRoot.gameObject.SetActive(false);
                                

                                Debug.Log($"Container {name} @ {Position}: Got an '{itemId}'");
                            }
                            else
                            {
                                Debug.LogError($"Container {name} @ {Position}: Could not instantiate a '{itemId}'");
                            }
                        }
                    }
                } else
                {
                    Debug.LogWarning($"Container @ {Position}: Inventory has no configuration, removing it");
                    DestroyImmediate(inventory);
                    inventory = null;
                } 
            } else
            {
                    Debug.LogWarning($"Container @ {Position}: Lacking inventory");
            }

            var displayInventory = GetComponent<WorldInventoryDisplay>();
            if (displayInventory != null)
            {
                displayInventory.Sync();
            }
        }


        private void OnEnable()
        {
            GridEntity.OnInteract += GridEntity_OnInteract;
        }

        private void OnDisable()
        {
            GridEntity.OnInteract -= GridEntity_OnInteract;
        }

        private bool AllowInteractBy(GridEntity entity)
        {
            if (direction == Direction.None)
            {
                // TODO: Doesn't account for thin walls...
                // Debug.Log($"Container @ {Position}: Checking if interaction is allowed Position.y({entity.Position.y == Position.y}) Distance({entity.Position.ManhattanDistance(Position)})");
                return entity.Position.y == Position.y && entity.Position.ManhattanDistance(Position) == 1;
            }

            // Debug.Log($"Container @ {Position}: Checking if interaction is allowed Position({entity.Position == Position}) Direction({direction == entity.LookDirection})");
            return entity.Position == Position && direction == entity.LookDirection;
        }

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if  (AllowInteractBy(entity))
            {
                if (phase == ContainerPhase.Locked)
                {
                    Debug.Log($"Container @ {Position}: {entity.name} is unlocking me");
                    HandleUnlock(entity);
                } else if (phase == ContainerPhase.Closed)
                {
                    Debug.Log($"Container @ {Position}: {entity.name} is opening me");
                    animator?.SetTrigger(OpenTrigger);
                    phase = ContainerPhase.Opened;
                } else if (phase == ContainerPhase.Opened)
                {
                    HandleLoot(entity);
                } else
                {
                    Debug.Log($"Container @ {Position}: {entity.name} attempted to interact with me but I'm a display cage");
                }
            }
        }

        void HandleUnlock(GridEntity entity)
        {
            var keyHolder = entity
                .GetComponentsInChildren<AbsInventory>()
                .FirstOrDefault(i => i.HasItem(key));

            if (keyHolder == null) {
                Debug.LogWarning($"Chest @ {Position}: requires key ({key})");
                return;
            }
            
            if (consumesKey && !keyHolder.Consume(key, out string _))
            {
                Debug.LogWarning($"Chest @ {Position}: Failed to consume {key} from {keyHolder}");
            }

            animator?.SetTrigger(UnlockOpenTrigger);
            phase = ContainerPhase.Opened;
        }

        // TODO: Make config or something
        bool autoLoot = true;
        void HandleLoot(GridEntity entity)
        {
            Debug.Log($"Container @ {Position}: Being looted by {entity.name}");
            if (autoLoot)
            {
                // TODO: Should have a main inventory really...
                var entityInventory = entity.GetComponent<AbsInventory>();
                if (entityInventory == null)
                {
                    Debug.LogWarning($"{entity.name} doesn't have an inventory");
                    return;
                }

                
                foreach (var item in inventory.Items.ToList())
                {
                    if (entityInventory.Add(item))
                    {
                        if (!inventory.Remove(item))
                        {
                            Debug.LogWarning($"Container @ {Position}: Failed to remove {item.Id}/{item.Origin}");
                        } else
                        {
                            Debug.Log($"Container @ {Position}: {entity.name} looted {item.Id}/{item.Origin}");
                        }
                    }
                }
            }
            // TODO: Handle show inventory ui
        }
    }
}
