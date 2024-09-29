using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using LMCore.TiledDungeon.Actions;
using LMCore.TiledDungeon.Integration;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDActuator : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        Vector3Int Coordinates;

        [SerializeField, HideInInspector]
        int[] groups = new int[0];

        [SerializeField, HideInInspector]
        bool repeatable;

        [SerializeField, HideInInspector]
        TDEnumInteraction interaction;

        [SerializeField, HideInInspector]
        Direction anchor;

        [SerializeField]
        AbstractDungeonAction[] pressActions;

        [SerializeField]
        AbstractDungeonAction[] dePressAction;

        bool automaticallyResets => dePressAction == null || dePressAction.Length == 0;

        bool active = true;
        bool lastActionWasPress = false;

        public override string ToString() =>
            $"Actuator {name} @ {Coordinates}/{anchor} Active({active}) LastWasPress({lastActionWasPress}) AutomaticReset({automaticallyResets}) Groups([{string.Join(", ", groups)}]) Repeatable({repeatable}) Interaction({interaction})";

        [ContextMenu("Info")]
        void Info() => Debug.Log(this);

        public void Configure(TDNode node)
        {
            var props = node
                .Config
                .GetObjectProps(o =>
                    o.Type == TiledConfiguration.instance.ObjActuatorClass &&
                    o.CustomProperties.Int(TiledConfiguration.instance.ObjGroupKey) > 0)
                .ToArray();

            interaction = props
                .FirstOrDefault(prop => prop.StringEnums.ContainsKey(TiledConfiguration.instance.InteractionKey))
                ?.Interaction(TiledConfiguration.instance.InteractionKey) ?? TDEnumInteraction.Interactable;

            anchor = GetComponent<Anchor>()?.CubeFace ??
                props
                .FirstOrDefault(prop => prop.StringEnums.ContainsKey(TiledConfiguration.instance.AnchorKey))
                ?.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() ?? Direction.None;

            groups = props
                .Select(prop => prop.Int(TiledConfiguration.instance.ObjGroupKey))
                .ToHashSet()
                .ToArray();

            repeatable = props
                .Any(prop => prop.Bool(TiledConfiguration.instance.ObjRepeatableKey));

            Coordinates = node.Coordinates;

            Debug.Log(this);
        }

        private void OnEnable()
        {
            if (interaction == TDEnumInteraction.Interactable)
            {
                GridEntity.OnInteract += GridEntity_OnInteract;
            }
            else if (interaction == TDEnumInteraction.Automatic)
            {
                TDNode.OnNewOccupant += TDNode_OnNewOccupant;
            }
        }

        private void TDNode_OnNewOccupant(TDNode node, GridEntity entity)
        {
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying) || 
                entity.Coordinates != Coordinates
                || entity.AnchorDirection != anchor)
            {
                var hadOccupant = occupants.Contains(entity);
                occupants.Remove(entity);

                if (lastActionWasPress && hadOccupant)
                {
                    Depress();
                }

                return;
            }

            bool wasEmpty = occupants.Count == 0;
            occupants.Add(entity);
            Debug.Log($"Actuator gains occupant {wasEmpty} {automaticallyResets} {lastActionWasPress}");

            if (automaticallyResets || (!lastActionWasPress && wasEmpty))
                Press();

        }

        private void OnDisable()
        {
            if (interaction == TDEnumInteraction.Interactable)
            {
                GridEntity.OnInteract -= GridEntity_OnInteract;
            }
            else if (interaction == TDEnumInteraction.Automatic)
            {
                TDNode.OnNewOccupant += TDNode_OnNewOccupant;
            }
        }

        HashSet<GridEntity> occupants = new HashSet<GridEntity>();

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (!active || entity.Coordinates != Coordinates) return;

            if (lastActionWasPress && !automaticallyResets)
            {
                Depress();
            }
            else
            {
                Press();
            }
        }

        ToggleGroup _toggleGroup;
        ToggleGroup ToggleGroup
        {
            get
            {
                if (_toggleGroup == null)
                {
                    _toggleGroup = GetComponentInParent<ToggleGroup>();
                }
                return _toggleGroup;
            }
        }

        void Press()
        {
            foreach (var group in groups)
            {
                ToggleGroup.Toggle(group);
                Debug.Log($"Actuator {name} @ {Coordinates} is toggling group: {group}");
            }

            foreach (var action in pressActions)
            {
                action.Play(null);
            }

            lastActionWasPress = true;

            active = repeatable;
            Debug.Log($"Actuator {name} @ {Coordinates} is {(active ? "active" : "inactive")} after interaction");
        }

        void Depress()
        {
            foreach (var action in dePressAction)
            {
                action.Play(null);
            }

            lastActionWasPress = false;
        }
    }
}
