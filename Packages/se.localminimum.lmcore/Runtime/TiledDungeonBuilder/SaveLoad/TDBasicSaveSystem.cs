using UnityEngine;

namespace LMCore.TiledDungeon.SaveLoad
{
    /// <summary>
    /// This limited version is a non-extended game save with no extra features
    /// </summary>
    public class BasicTDSaveSystem : TDSaveSystem<GameSave, BasicTDSaveSystem>
    {
        public override GameSave saveData { get; protected set; }

        [SerializeField]
        string LevelManagerResource;

        [SerializeField]
        string FirstLevelSceneName;

        TDLevelManager levelManager;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            levelManager = TDLevelManager.InstanceOrResource(LevelManagerResource);
        }

        [ContextMenu("Log Status")]
        public override void LogStatus()
        {
            base.LogStatus();
        }

        [ContextMenu("AutoSave")]
        public void AutoSave()
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
            var startLoadingSave = levelManager.LoadSceneAsync();
            if (startLoadingSave == null) return;

            var player = FindFirstObjectByType<TDPlayerEntity>();
            if (player == null)
            {
                Debug.LogError(PrefixLogMessage("Without a player I don't know what scene to unload!"));
            }
            ;

            LoadSaveAsync(
                0,
                (save, loadSave) =>
                    startLoadingSave(
                        player.gameObject.scene,
                        save.player.levelName,
                        completeLoading =>
                        {
                            loadSave();
                            completeLoading();
                        }),
                () => startLoadingSave(player.gameObject.scene, FirstLevelSceneName, null)
            );
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
