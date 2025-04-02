using LMCore.AbstractClasses;
using LMCore.IO;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDEnemyPool : Singleton<TDEnemyPool, TDEnemyPool>, IOnLoadSave
    {
        [SerializeField]
        List<TDEnemy> EnemyPrefabs = new List<TDEnemy>();

        protected string PrefixLogMessage(string message) =>
            $"EnemyPool: {message}";

        public TDEnemy GetInstance(string classId, Transform parent)
        {
            var prefab = EnemyPrefabs.FirstOrDefault(p => p.ClassId == classId);
            if (prefab == null)
            {
                Debug.LogError(PrefixLogMessage($"I don't have an enemy of class {classId}"));
                return null;
            }
            return Instantiate(prefab, parent);
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            TiledDungeon.OnDungeonLoad += TiledDungeon_OnDungeonLoad;
        }

        private void OnDisable()
        {
            TiledDungeon.OnDungeonLoad -= TiledDungeon_OnDungeonLoad;
        }

        TiledDungeon _Dungeon;
        TiledDungeon Dungeon
        {
            get
            {
                if (_Dungeon == null)
                {
                    _Dungeon = FindFirstObjectByType<TiledDungeon>();
                }
                return _Dungeon;
            }
        }

        private void TiledDungeon_OnDungeonLoad(TiledDungeon dungeon, bool fromSave)
        {
            _Dungeon = dungeon;
        }

        List<EnemyDeathData> deadEnemies = new List<EnemyDeathData>();

        public void RegisterDeath(TDEnemy enemy)
        {
            deadEnemies.Add(new EnemyDeathData()
            {
                MapName = Dungeon?.MapName,
                ClassId = enemy.ClassId,
                Id = enemy.Id,
            });
        }

        #region Load/Save
        public List<EnemyDeathData> Save() => new List<EnemyDeathData>(deadEnemies);

        public int OnLoadPriority => 50;

        private void OnLoadGameSave(GameSave save)
        {
            if (save?.deadEnimies == null)
            {
                deadEnemies.Clear();
                return;
            }

            deadEnemies = new List<EnemyDeathData>(save.deadEnimies);
            foreach (var deathData in deadEnemies.Where(e => e.MapName == Dungeon.MapName))
            {
                var entity = Dungeon.RemoveGridEntityFromDungeon((entity) =>
                {
                    var other = entity.GetComponentInChildren<TDEnemy>();
                    if (other == null) return false;
                    return other.ClassId == deathData.ClassId && other.Id == deathData.Id;
                });

                if (entity != null)
                {
                    var enemy = entity.GetComponent<TDEnemy>();
                    enemy.Stats.Kill();
                    // This notices that enemy is dead and responds accordingly
                    enemy.UpdateActivity();
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
        #endregion

    }
}
