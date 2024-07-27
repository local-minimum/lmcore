using System.Linq;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Inventory;
using LMCore.TiledDungeon.Integration;

namespace LMCore.TiledDungeon
{
    public class TDChest : MonoBehaviour
    {
        enum ChestPhase { Locked, Closed, Opened };

        [SerializeField]
        Animator animator;

        [SerializeField]
        string UnlockOpenTrigger;

        [SerializeField]
        string OpenTrigger;

        [SerializeField, HideInInspector]
        Vector3Int Position;

        [SerializeField, HideInInspector]
        ChestPhase phase;

        [SerializeField, HideInInspector]
        Direction direction;

        [SerializeField, HideInInspector]
        string key;

        [SerializeField, HideInInspector]
        bool consumesKey;

        TDNode _node;
        TDNode node
        {
            get
            {
                if (_node == null)
                {
                    _node = GetComponentInParent<TDNode>();
                }
                return _node;
            }
        }

        public bool BlockingPassage => true;

        public void Configure(
            TDNode node,
            Vector3Int position,
            Direction direction,
            TileModification[] modifications
            )
        {
            Position = position;

            var props = modifications.FirstOrDefault(mod =>
                mod.Tile.Type == TiledConfiguration.instance.ChestClass)?.Tile
                .CustomProperties;

            var interaction = props == null ? TDEnumInteraction.Closed : props.Interaction(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Closed);
            switch (interaction)
            {
                case TDEnumInteraction.Open:
                    phase = ChestPhase.Opened; 
                    break;
                case TDEnumInteraction.Locked: 
                    phase = ChestPhase.Locked; 
                    break;
                case TDEnumInteraction.Closed:
                    phase = ChestPhase.Closed;
                    break;
                default:
                    Debug.LogError($"Chest @ {Position}: Recieved interaction {interaction} which it doesn't know how to do");
                    phase = ChestPhase.Closed;
                    break;
            }

            this.direction = direction;

            key = node.FirstObjectValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props?.String(TiledConfiguration.instance.KeyKey)
            );

            consumesKey = node.FirstObjectValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.ConusumesKeyKey)
            );

            // TODO: Handle inventory
        }

        private void OnEnable()
        {
            GridEntity.OnInteract += GridEntity_OnInteract;
        }

        private void OnDisable()
        {
            GridEntity.OnInteract -= GridEntity_OnInteract;
        }

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (direction.Inverse().Translate(entity.Position) == Position)
            {
                if (phase == ChestPhase.Locked)
                {
                    HandleUnlock(entity);
                } else if (phase == ChestPhase.Closed)
                {
                    animator.SetTrigger(OpenTrigger);
                    phase = ChestPhase.Opened;
                } else
                {
                    HandleLoot(entity);
                }
            }
        }

        void HandleUnlock(GridEntity entity)
        {
            var keyHolder = entity
                .GetComponentsInChildren<AbsInventory>()
                .FirstOrDefault(i => i.HasItem(TiledConfiguration.instance.KeyKey, key));

            if (keyHolder == null) {
                Debug.LogWarning($"Chest @ {Position}: requires key ({key})");
                return;
            }
            
            if (consumesKey && !keyHolder.Consume(TiledConfiguration.instance.KeyKey, key))
            {
                Debug.LogWarning($"Chest @ {Position}: Failed to consume {key} from {keyHolder}");
            }

            animator.SetTrigger(UnlockOpenTrigger);
            phase = ChestPhase.Opened;
        }

        void HandleLoot(GridEntity entity)
        {
            // TODO: Handle looting
        }
    }
}
