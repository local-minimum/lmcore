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

            anchor = props
                .FirstOrDefault(prop => prop.StringEnums.ContainsKey(TiledConfiguration.instance.AnchorKey))
                ?.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() ?? Direction.None;

            groups = props
                .Select(prop => prop.Int(TiledConfiguration.instance.ObjGroupKey))
                .ToHashSet()
                .ToArray();

            repeatable = props
                .Any(prop => prop.Bool(TiledConfiguration.instance.ObjRepeatableKey));

            Coordinates = node.Coordinates;

            Debug.Log($"Actuator @ {Coordinates}: Interaction({interaction}) Anchor({anchor}) Groups([{string.Join(", ", groups)}]) Repeatable({repeatable})");
        }

        private void OnEnable()
        {
            if (interaction == TDEnumInteraction.Interactable)
            {
                GridEntity.OnInteract += GridEntity_OnInteract;
            }
            else if (interaction == TDEnumInteraction.Automatic)
            {
                foreach (var mover in Movers.movers)
                {
                    mover.OnMoveStart += Mover_OnMoveStart;
                    mover.OnMoveEnd += Mover_OnMoveEnd;
                }

                Movers.OnActivateMover += Movers_OnActivateMover;
                Movers.OnDeactivateMover += Movers_OnDeactivateMover;
            }
        }
        private void OnDisable()
        {
            if (interaction == TDEnumInteraction.Interactable)
            {
                GridEntity.OnInteract -= GridEntity_OnInteract;
            }
            else if (interaction == TDEnumInteraction.Automatic)
            {
                foreach (var mover in Movers.movers)
                {
                    mover.OnMoveStart -= Mover_OnMoveStart;
                    mover.OnMoveEnd -= Mover_OnMoveEnd;
                }

                Movers.OnActivateMover -= Movers_OnActivateMover;
                Movers.OnDeactivateMover -= Movers_OnDeactivateMover;
            }
        }

        private void Movers_OnActivateMover(IEntityMover mover)
        {
            mover.OnMoveStart += Mover_OnMoveStart;
            mover.OnMoveEnd += Mover_OnMoveEnd;
        }

        private void Movers_OnDeactivateMover(IEntityMover mover)
        {
            mover.OnMoveStart -= Mover_OnMoveStart;
            mover.OnMoveEnd -= Mover_OnMoveEnd;
        }


        HashSet<GridEntity> occupants = new HashSet<GridEntity>();
        private void Mover_OnMoveStart(GridEntity entity, List<Vector3Int> positions, List<Direction> anchors)
        {
            if (!active) return;
            var passesPlate = positions
                .Zip(anchors, (pos, anch) => new { pos, anch })
                .Skip(1)
                .Any(state => state.pos == Coordinates && state.anch == anchor);

            if (passesPlate)
            {
                bool wasEmpty = occupants.Count == 0;
                occupants.Add(entity);

                if (automaticallyResets || (!lastActionWasPress && wasEmpty))
                    Press();
            }
        }

        private void Mover_OnMoveEnd(GridEntity enity, bool successful)
        {
            if (!active) return;
            if (occupants.Contains(enity) && (enity.Coordinates != Coordinates || enity.AnchorDirection != anchor))
            {
                occupants.Remove(enity);

                if (lastActionWasPress)
                {
                    Depress();
                }
            }
        }

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
