using LMCore.Extensions;
using LMCore.Juice;
using LMCore.TiledDungeon.SaveLoad;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SampleMenu : MonoBehaviour
{
    [SerializeField]
    string SaveSystemResource = "SaveSystem";

    [SerializeField]
    string MenuScene = "SampleMenuScene";

    [SerializeField]
    string LevelSceneName = "SampleScene";

    [SerializeField]
    string LoadingSceneName = "SampleLoadingScene";

    [SerializeField]
    GameObject ContinueButton;

    AsyncOperation LoadingScene;
    private void Start()
    {
        ContinueButton?.SetActive(
            BasicTDSaveSystem.InstanceOrResource(SaveSystemResource).HasSave(0)
        );
        LoadingScene = SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);
    }

    Transition _sceneTransition = null;

    Transition SceneTransition
    {
        get
        {
            if (_sceneTransition != null) return _sceneTransition;

            if (LoadingScene == null || !LoadingScene.isDone) return null;

            _sceneTransition = this.GetFirstInScene<Transition>(LoadingSceneName);

            return _sceneTransition;
        }
    }
    
    void ShowLoading()
    {
        if (SceneTransition != null)
        {
            SceneTransition.ActivePhase = Transition.Phase.EaseIn;
        }
    }

    bool readyToComplete;

    void HideLoading()
    {
        if (readyToComplete && SceneTransition?.ActivePhase == Transition.Phase.Waiting)
        {
            SceneTransition.ActivePhase = Transition.Phase.EaseOut;

            var myAudioListener = this.GetFirstInScene<AudioListener>();
            if (myAudioListener)
            {
                myAudioListener.enabled = false;
            }

            if (levelAudioListener)
            {
                levelAudioListener.enabled = true;
            }
            var unloadMenus = SceneManager.UnloadSceneAsync(MenuScene);

            unloadMenus.completed += UnloadMenus_completed;
        }
    }


    AsyncOperation LevelScene;

    bool useAutoSave;

    public void LoadLevel(bool useAutoSave)
    {
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.gameObject.SetActive(false);
        }

        // Time.timeScale = 0;
        this.useAutoSave = useAutoSave;
        ShowLoading();
        LevelScene = SceneManager.LoadSceneAsync(LevelSceneName, LoadSceneMode.Additive);
        LevelScene.completed += LevelScene_completed;
    }

    void Update()
    {
        if (LevelScene == null) return;

        if ((SceneTransition?.ActivePhase ?? Transition.Phase.Inacive) == Transition.Phase.Inacive)
        {
            ShowLoading();
            return;
        }

        if (readyToComplete)
        {
            HideLoading();

            if (SceneTransition?.ActivePhase == Transition.Phase.Inacive)
            {
                readyToComplete = false;
                // Time.timeScale = 1;
                Debug.Log("Releasing player"); 
            }
        }
    }

    AudioListener levelAudioListener;

    private void LevelScene_completed(AsyncOperation obj)
    {
        var levelScene = SceneManager.GetSceneByName(LevelSceneName);
        Debug.Log($"Setting active scene to {levelScene.name}");
        SceneManager.SetActiveScene(levelScene);

        levelAudioListener = this.GetFirstInScene<AudioListener>(LevelSceneName);
        levelAudioListener.enabled = false;

        if (useAutoSave)
        {
            var saveSystem = this.GetFirstInScene<BasicTDSaveSystem>(LevelSceneName);

            if (saveSystem != null)
            {
                Debug.Log("Spawning savesystem");
                saveSystem = BasicTDSaveSystem.InstanceOrResource(SaveSystemResource);
            }
            saveSystem.Load(0, HandleLevelLoaded, HandleLevelFailed);
        } else
        {
            HandleLevelLoaded();
        }
    }

    void HandleLevelLoaded()
    {
        Debug.Log("Level Loaded");
        readyToComplete = true;
        HideLoading();
    }

    private void UnloadMenus_completed(AsyncOperation obj)
    {
        Debug.Log("Menu unloaded");
    }

    void HandleLevelFailed()
    {
        Debug.LogError("Failed to load level");
    }
}
