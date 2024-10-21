using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using LMCore.TiledDungeon.Actions;
using LMCore.TiledDungeon.Integration;
using UnityEngine;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;

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

        [SerializeField]
        AbstractDungeonAction[] pressActions;

        [SerializeField]
        AbstractDungeonAction[] dePressAction;

        bool automaticallyResets => dePressAction == null || dePressAction.Length == 0;

        bool active = true;
        bool lastActionWasPress = false;

        public override string ToString() =>
            $"Actuator {name} @ {Coordinates}/{Anchor.CubeFace} Active({active}) LastWasPress({lastActionWasPress}) AutomaticReset({automaticallyResets}) Groups([{string.Join(", ", groups)}]) Repeatable({repeatable}) Interaction({interaction})";
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
                GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
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
                GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
            }
        }

        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            var flying = entity.TransportationMode.HasFlag(TransportationMode.Flying);
            var onMe = entity.Coordinates == Coordinates && entity.AnchorDirection == Anchor.CubeFace;
            if (occupants.Contains(entity))
            {
                if (flying || !onMe)
                {
                    occupants.Remove(entity);
                }

                if (occupants.Count() == 0 && lastActionWasPress)
                {
                    Depress();
                }
            } else if (onMe && !flying)
            {
                occupants.Add(entity);
                if (occupants.Count() == 1 && (automaticallyResets || !lastActionWasPress))
                {
                    Press();
                }
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
