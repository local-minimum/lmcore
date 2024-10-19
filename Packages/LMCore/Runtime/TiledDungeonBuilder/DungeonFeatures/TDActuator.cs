using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using LMCore.TiledDungeon.Actions;
using LMCore.TiledDungeon.Integration;
using UnityEngine;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;

// TODO: Save for this one (press state, and active state and such)
// And make save for moving plattis too
namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDActuator : TDFeature, IOnLoadSave
    {
        /// <summary>
        /// Actuator can be interacted with many times
        /// </summary>
        [SerializeField, HideInInspector]
        bool repeatable;

        /// <summary>
        /// Toggle groups that pressing (and potentially depressing) triggers
        /// </summary>
        [SerializeField, HideInInspector]
        int[] groups = new int[0];

        /// <summary>
        /// The ability to invoke toggle groups on unset
        /// </summary>
        [SerializeField, HideInInspector]
        bool invokeToggleGroupOnUnset;

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
        protected string PrefixLogMessage(string message) =>
            $"Actuator {name} @ {Coordinates}: {message}";

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

            anchor = Anchor?.CubeFace ??
                props
                .FirstOrDefault(prop => prop.StringEnums.ContainsKey(TiledConfiguration.instance.AnchorKey))
                ?.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() ?? Direction.None;

            groups = props
                .Select(prop => prop.Int(TiledConfiguration.instance.ObjGroupKey))
                .ToHashSet()
                .ToArray();

            repeatable = props
                .Any(prop => prop.Bool(TiledConfiguration.instance.ObjRepeatableKey));

            invokeToggleGroupOnUnset = props
                .Any(prop => prop.Bool(TiledConfiguration.instance.ObjRepeatableKey));

            Debug.Log(this);
        }

        private void Start()
        {
            InitStartCoordinates();
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

                if (hadOccupant) { 
                    if (lastActionWasPress && occupants.Count() == 0)
                    {
                        Depress();
                    }
                    else
                    {
                        Debug.LogWarning(
                            PrefixLogMessage($"Entity {entity.name} moved away but last action was press({lastActionWasPress}) and occupant ({occupants.Count()})"));
                    }
                }

                return;
            }

            bool wasEmpty = occupants.Count == 0;
            occupants.Add(entity);

            if (automaticallyResets || (!lastActionWasPress && wasEmpty))
            {
                Debug.Log(PrefixLogMessage($"Gained occupant {wasEmpty} {automaticallyResets} {lastActionWasPress}"));
                Press();
            } else
            {
                Debug.LogWarning(PrefixLogMessage($"Could not press: wasEmpty({wasEmpty}), last was press({lastActionWasPress}), automatic reset ({automaticallyResets})"));
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
                TDNode.OnNewOccupant -= TDNode_OnNewOccupant;
            }
        }

        HashSet<GridEntity> occupants = new HashSet<GridEntity>();

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (!active || entity.Coordinates != Coordinates) return;

            if (lastActionWasPress && !automaticallyResets)
            {
                Debug.Log(PrefixLogMessage("Entity depresses actuator"));
                Depress();
            }
            else
            {
                Debug.Log(PrefixLogMessage("Entity presses actuator"));
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

        public int OnLoadPriority => 500;

        void Press()
        {
            foreach (var group in groups)
            {
                Debug.Log(PrefixLogMessage($"Is toggling group: {group}"));
                ToggleGroup.Toggle(group);
            }

            foreach (var action in pressActions)
            {
                action.Play(null);
            }

            lastActionWasPress = true;

            active = repeatable;
            Debug.Log(PrefixLogMessage($"Is {(active ? "active" : "inactive")} after interaction"));
        }

        void Depress()
        {
            foreach (var action in dePressAction)
            {
                action.Play(null);
            }

            if (invokeToggleGroupOnUnset)
            {
                foreach (var group in groups)
                {
                    Debug.Log(PrefixLogMessage($"Is toggling group via automatic unset: {group}"));
                    ToggleGroup.Toggle(group);
                }
            }

            lastActionWasPress = false;
        }

        public KeyValuePair<Vector3Int, ActuatorSave> Save() =>
            new KeyValuePair<Vector3Int, ActuatorSave>(
                StartCoordinates,
                new ActuatorSave() { 
                    active = active,
                    lastActionWasPress = lastActionWasPress,
                });

        private void OnLoadGameSave(GameSave save)
        {
            if (save == null) return;

            var lvl = GetComponentInParent<IDungeon>().MapName;
            var actuatorSave = save.levels[lvl]?.actuators.GetValueOrDefault(StartCoordinates);

            if (actuatorSave == null)
            {
                Debug.LogError(PrefixLogMessage("I have no saved state"));
                return; 
            }

            active = actuatorSave.active;
            lastActionWasPress = actuatorSave.lastActionWasPress;

            if (lastActionWasPress && !automaticallyResets)
            {
                foreach (var action in pressActions)
                {
                    action.Play();
                    action.Finalise(false);
                }
            } else
            {
                foreach (var action in dePressAction)
                {
                    action.Play();
                    action.Finalise(false);
                }
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
