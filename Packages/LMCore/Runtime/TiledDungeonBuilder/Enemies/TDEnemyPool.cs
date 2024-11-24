using LMCore.AbstractClasses;
using LMCore.TiledDungeon.Enemies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDEnemyPool : Singleton<TDEnemyPool, TDEnemyPool>
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
            }
            return Instantiate(prefab, parent);
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
