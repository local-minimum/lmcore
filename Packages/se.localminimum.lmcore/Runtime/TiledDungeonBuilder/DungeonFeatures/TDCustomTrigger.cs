using LMCore.Crawler;
using LMCore.EntitySM.State;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.TiledImporter;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    /// <summary>
    /// A point or rect in Tiled may cause the addition of this if its
    /// class matches the TiledConfiguraiton.ObjCustomClass and the
    /// string value of TiledConfiguration.ObjCustomIdKey matches a
    /// prefab in TDCustomContent with a TDCustomTrigger on the prefab root.
    /// 
    /// Properties that are recognized
    /// - "Player" (bool): If triggering on player actions
    /// - "Trigger" (Transition) What action triggers (normally entry and/or exit)
    /// - "EnemyId" (string) Enemy to be affected by trigger, if any
    /// - "EnemyState" (string) State that enemy should enter if triggered
    /// - "Repeatable" (boolean) If it can be triggered more than once, default is false
    /// - "SaveId" (string) If multiple triggers exist for same coordinates, this identifies
    ///   which save is associated with which trigger. Can be omitted if there's just one
    ///   trigger for the coordinates
    /// </summary>
    public class TDCustomTrigger : TDFeature, ITDCustom, IOnLoadSave
    {
        static List<TDCustomTrigger> triggers = new List<TDCustomTrigger>();

        [SerializeField, HideInInspector]
        bool _triggerPlayer;

        [SerializeField, HideInInspector]
        TDEnumTransition _triggerOn;

        [SerializeField, HideInInspector]
        string _enemyId;

        [SerializeField, HideInInspector]
        StateType _enemyState;

        [SerializeField, HideInInspector]
        bool _repeatable;

        #region Save State
        [SerializeField, HideInInspector]
        string _saveId;
        bool triggered;
        #endregion

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"TriggerPlayer({_triggerPlayer}) on {_triggerOn} -> Enemy({_enemyId}/{_enemyState}) Repeatable({_repeatable}), Triggered({triggered})");
        }

        public void Configure(TDNode node, TiledCustomProperties properties)
        {
            _triggerPlayer = properties.Bool("Player", false);
            _triggerOn = properties.Transition("Trigger", TDEnumTransition.None);
            _enemyId = properties.String("EnemyId", null);
            _enemyState = StateTypeExtensions.From(properties.String("EnemyState", null));
            _repeatable = properties.Bool("Repeatable", false);
            _saveId = properties.String("SaveId", null);
        }

        bool ValidTriggerOn => _triggerOn.HasEntry() || _triggerOn.HasExit();

        private void OnEnable()
        {
            if (_triggerPlayer && ValidTriggerOn)
            {
                GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
            }
            triggers.Add(this);
        }

        private void OnDisable()
        {
            if (_triggerPlayer && ValidTriggerOn)
            {
                GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
            }
            triggers.Remove(this);
        }

        void OnDestroy()
        {
            triggers.Remove(this);
        }

        Dictionary<GridEntity, bool> previuslyHere = new Dictionary<GridEntity, bool>();
        bool PreviouslyHere(GridEntity entity) =>
            previuslyHere.TryGetValue(entity, out bool value) ? value : false;

        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            if (triggered && !_repeatable) return;

            if (entity.EntityType == GridEntityType.PlayerCharacter && _triggerPlayer)
            {
                bool isHere = entity.Coordinates == Coordinates;
                if (_triggerOn.HasEntry() && isHere)
                {
                    TriggerEvent();
                }
                else if (_triggerOn.HasExit() && PreviouslyHere(entity) && !isHere)
                {
                    TriggerEvent();
                }
                previuslyHere[entity] = isHere;
            }
        }

        GridEntity _Enemy;
        GridEntity Enemy
        {
            get
            {
                if (_Enemy == null)
                {
                    var e = Dungeon.GetEntity(_enemyId);
                    if (e != null && e.EntityType == GridEntityType.Enemy)
                    {
                        _Enemy = e;
                    }
                    else
                    {
                        Debug.Log($"'{e}' ({_enemyId}) is not an enemy");
                    }
                }
                return _Enemy;
            }
        }

        void TriggerEvent()
        {
            Debug.Log($"Trigger {_enemyId} {Enemy} to {_enemyState}");

            if (!string.IsNullOrEmpty(_enemyId) && _enemyState != StateType.None)
            {
                var enemyEntity = Enemy;
                if (enemyEntity != null)
                {
                    var enemy = enemyEntity.GetComponent<TDEnemy>();
                    if (enemy != null) enemy.ForceActivity(_enemyState);
                }
            }
        }

        #region Load/Save
        public CustomTriggerSave Save() =>
            triggered ? new CustomTriggerSave()
            {
                Coordinates = Coordinates,
                SaveId = _saveId,
                Triggered = true
            } : null;

        public static IEnumerable<CustomTriggerSave> CollectSaves(TiledDungeon dungeon) =>
            triggers
                .Where(t => t.Dungeon == dungeon)
                .Select(t => t.Save())
                .Where(s => s != null);

        // As with most world things must be after moving platforms
        // which are 10
        public int OnLoadPriority => 9;

        void OnLoadGameSave(GameSave save)
        {
            if (save == null) return;

            var lvl = Dungeon.MapName;
            var triggerSave = save.levels[lvl]?.customTriggers
                .FirstOrDefault(s => s.Coordinates == Coordinates && (string.IsNullOrEmpty(s.SaveId) || s.SaveId == _saveId));

            triggered = triggerSave?.Triggered ?? false;
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
        #endregion
    }
}
