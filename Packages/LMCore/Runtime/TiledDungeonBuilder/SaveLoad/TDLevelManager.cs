using LMCore.AbstractClasses;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.Juice;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LMCore.TiledDungeon
{
    using SaveLoader = System.Action<System.Action>;

    public class TDLevelManager : Singleton<TDLevelManager, TDLevelManager> 
    {
        public delegate void SceneLoadedEvenet(string sceneName);

        public static event SceneLoadedEvenet OnSceneLoaded;

        [SerializeField]
        string LoadingSceneName;

        [SerializeField]
        SerializableDictionary<string, string> levelsToScenes = new SerializableDictionary<string, string>();
        string levelToLoad;
        string levelSceneName => levelsToScenes.GetValueOrDefault(levelToLoad);

        Transition loadingEffect;

        protected string PrefixLogMessage(string message) => $"Level Manager: {message}";

        private bool transitioning = false;
        bool readyToFinalizeLoading;
        Scene unloadingScene;
        bool sourceSceneUnloaded;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        EventSystem unloadingSceneEventSystem;

        void DisableEventSystem(Scene sourceScene)
        {
            unloadingSceneEventSystem = this.GetFirstInScene<EventSystem>(sourceScene, sys => sys.gameObject.activeSelf);
            if (unloadingSceneEventSystem != null )
            {
                unloadingSceneEventSystem.gameObject.SetActive(false);
            }
        }

        Camera DisableCamera(string sceneName) => DisableCamera(SceneManager.GetSceneByName(sceneName));
        Camera DisableCamera(Scene scene)
        {
            var camera = this.GetFirstInScene<Camera>(scene);
            if (camera != null)
            {
                camera.enabled = false;
            }
            return camera;
        }

        /// <summary>
        /// Start a loading transition, supplying needed information later
        /// </summary>
        public System.Action<Scene, string, SaveLoader> LoadSceneAsync() 
        {
            if (transitioning)
            {
                Debug.LogError(PrefixLogMessage($"Can't initiate loading since alreay busy loading"));
                return null;
            }

            transitioning = true;
            levelToLoad = null;
            saveLoader = null;
            levelLoadedStarted = false;
            sourceSceneUnloaded = false;

            InitiateLoadingTransitionScene();

            return StartLoading;
        }

        void StartLoading(Scene unloadingScene, string levelToLoad, SaveLoader saveLoader) {
            this.unloadingScene = unloadingScene;
            this.levelToLoad = levelToLoad;
            this.saveLoader = saveLoader;

            DisableEventSystem(unloadingScene);

            Debug.Log(PrefixLogMessage($"Loading '{levelToLoad}'/'{levelSceneName}' from '{unloadingScene.name}' with save state: {saveLoader != null}"));
            InitiateUnloadSourceScene();
        }

        private void InitiateLoadingTransitionScene()
        {
            Debug.Log(PrefixLogMessage("Loading transition scene"));
            var loadingSceneOperation = SceneManager
                .LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);
            loadingSceneOperation.completed += LoadingSceneOperation_completed;
        }

        private void InitiateUnloadSourceScene()
        {
            if (sourceSceneUnloaded || transitionPhase != Transition.Phase.Waiting) return;

            Debug.Log(PrefixLogMessage($"Unloading source scene {unloadingScene.name}"));
            sourceSceneUnloaded = true;
            var unloadSceneOperation = SceneManager.UnloadSceneAsync(unloadingScene);
            unloadSceneOperation.completed += UnloadSceneOperation_completed;
        }

        private void InitiateLevelSceneLoading()
        {
            if (levelLoadedStarted || transitionPhase != Transition.Phase.Waiting)
            {
                Debug.Log(PrefixLogMessage($"Ignoring level load init because started({levelLoadedStarted}) or wrong phase ({transitionPhase})"));
                return;
            }

            Debug.Log(PrefixLogMessage("Loading target level scene"));
            levelLoadedStarted = true;
            var levelLoadingOperation = SceneManager
                .LoadSceneAsync(levelSceneName, LoadSceneMode.Additive);
            levelLoadingOperation.completed += LevelLoadingOperation_completed;
        }

        private void SwapAudioListenerToLoadingScene() {
            var loadingListener = this.GetFirstInScene<AudioListener>(LoadingSceneName);
            if (loadingListener != null)
            {
                DisableLevelAudioListener(unloadingScene);

                loadingListener.enabled = true;
            }
        }

        private void LoadingSceneOperation_completed(AsyncOperation obj)
        {
            loadinSceneCamera = DisableCamera(LoadingSceneName);
            SwapAudioListenerToLoadingScene();

            loadingEffect = this.GetFirstInScene<Transition>(LoadingSceneName);

            Debug.Log(PrefixLogMessage("Loading scene loaded"));

            if (loadingEffect == null)
            {
                Debug.LogError(PrefixLogMessage($"Loading scene '{LoadingSceneName}' does not have any transition effect"));
                transitionPhase = Transition.Phase.Waiting;
                InitiateUnloadSourceScene();
            } else
            {
                loadingEffect.OnPhaseChange += LoadingEffect_onPhaseChange;
                loadingEffect.ActivePhase = Transition.Phase.EaseIn;
            }
        }

        Transition.Phase transitionPhase;

        private void LoadingEffect_onPhaseChange(Transition.Phase phase)
        {
            transitionPhase = phase;

            Debug.Log(PrefixLogMessage($"Transition phase is {transitionPhase}"));

            if (transitionPhase == Transition.Phase.Waiting)
            {
                InitiateUnloadSourceScene();
            } else if (readyToFinalizeLoading || phase == Transition.Phase.Completed) {
                FinalizeTransition();
            }
        }

        bool levelLoadedStarted;

        AudioListener levelSceneAudioListerner;
        /// <summary>
        /// To avoid duplicate audio listeners while transitioning it is disabled
        /// </summary>
        AudioListener DisableLevelAudioListener(Scene scene)
        {
            var listener = this.GetFirstInScene<AudioListener>(scene);
            if (listener == null && listener.enabled)
            {
                listener.enabled = false;
                return listener;
            }
            return null;
        }

        SaveLoader saveLoader;
        Camera loadinSceneCamera;

        private void LevelLoadingOperation_completed(AsyncOperation obj)
        {
            DisableCamera(LoadingSceneName);
            var levelScene = SceneManager.GetSceneByName(levelSceneName);
            SceneManager.SetActiveScene(levelScene);
            levelSceneAudioListerner = DisableLevelAudioListener(levelScene);

            Debug.Log(PrefixLogMessage("Target scene loaded"));

            if (saveLoader != null)
            {
                saveLoader(HandleLoadingLevelComplete);
            } else
            {
                HandleLoadingLevelComplete();
            }
        }

        void HandleLoadingLevelComplete()
        {
            Debug.Log(PrefixLogMessage("Target scene ready"));
            if (loadingEffect != null)
            {
                loadingEffect.ActivePhase = Transition.Phase.EaseOut;
            } else
            {
                FinalizeTransition();
            }
        }

        void SwapToLoadingLevelAudioListerner()
        {
            var loadingListeners = this.GetFirstInScene<AudioListener>(LoadingSceneName);

            if (loadingListeners != null)
            {
                loadingListeners.enabled = false;
            }

            if (levelSceneAudioListerner != null)
            {
                levelSceneAudioListerner.enabled = true;
            }
        }


        private void UnloadSceneOperation_completed(AsyncOperation obj)
        {
            Debug.Log(PrefixLogMessage("Source scene unloaded"));
            loadinSceneCamera.enabled = true;
            InitiateLevelSceneLoading();
        }

        private void FinalizeTransition()
        {
            Debug.Log(PrefixLogMessage($"Loading of {LoadingSceneName} completed"));
            SwapToLoadingLevelAudioListerner();
            OnSceneLoaded?.Invoke(LoadingSceneName);
            SceneManager.UnloadSceneAsync(LoadingSceneName).completed += TDLevelManager_completed;
        }

        private void TDLevelManager_completed(AsyncOperation obj)
        {
            transitioning = false;
            Debug.Log(PrefixLogMessage($"Transition completed for '{levelSceneName}'"));
        }
    }
}
