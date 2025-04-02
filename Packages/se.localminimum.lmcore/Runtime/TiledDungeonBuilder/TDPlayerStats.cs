using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDPlayerStats : MonoBehaviour, IOnLoadSave
    {
        float totalTime;

        float sesstionStart;
        int steps;
        int turns;

        public int OnLoadPriority => 103;

        GridEntity entity;
        GridEntity Entity
        {
            get
            {
                if (entity == null)
                {
                    entity = GetComponentInParent<GridEntity>();
                }
                return entity;
            }
        }

        private void OnEnable()
        {
            sesstionStart = Time.realtimeSinceStartup;
            Entity.Input.OnMovement += Input_OnMovement;
        }

        private void OnDisable()
        {
            totalTime += Time.realtimeSinceStartup - sesstionStart;
            Entity.Input.OnMovement -= Input_OnMovement;
        }

        private void Input_OnMovement(int tickId, Movement movement, float duration, bool forced)
        {
            if (movement.IsTranslation())
            {
                steps++;
            }
            else if (movement.IsRotation())
            {
                turns++;
            }
        }

        public PlayerStatsSave Save()
        {
            totalTime += Time.realtimeSinceStartup - sesstionStart;
            sesstionStart = Time.realtimeSinceStartup;

            return new PlayerStatsSave()
            {
                steps = steps,
                turns = turns,
                playTime = totalTime
            };
        }

        public void OnLoadSaveGame(GameSave save)
        {
            if (save == null) return;

            if (save.playerStats == null)
            {
                totalTime = 0;
                steps = 0;
                turns = 0;
            }
            else
            {
                totalTime = save.playerStats.playTime;
                steps = save.playerStats.steps;
                turns = save.playerStats.turns;
            }


            sesstionStart = Time.realtimeSinceStartup;
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadSaveGame(save as GameSave);
            }
        }
    }
}
