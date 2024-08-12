using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    /// <summary>
    /// This limited version is a non-extended game save with no extra features
    /// </summary>
    public class BasicTDSaveSystem : TDSaveSystem<GameSave, BasicTDSaveSystem> 
    {
        public override GameSave saveData { get; protected set; }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        [ContextMenu("Log Status")]
        public override void LogStatus()
        {
            base.LogStatus();
        }

        [ContextMenu("AutoSave")]
        void AutoSave()
        {
            Save(
                0,
                () => Debug.Log(PrefixLogMessage("Auto Saved 0 ")),
                () => Debug.Log(PrefixLogMessage("Auto Save Failed 0"))
            );
        }

        [ContextMenu("Load AutoSave")]
        void LoadAutoSave()
        {
            Load(
                0,
                () => Debug.Log(PrefixLogMessage("Loaded Auto Save 0")),
                () => Debug.Log(PrefixLogMessage("Failed to load Auto Save 0")));
        }


        [ContextMenu("Wipe All Saves")]
        void Wipe()
        {
            DeleteAllSaves();
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            Destroy(gameObject);
        }
    }
}
