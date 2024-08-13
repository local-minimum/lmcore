using LMCore.AbstractClasses;
using LMCore.Extensions;
using LMCore.Juice;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LMCore.TiledDungeon
{
    using SaveLoader = System.Action;

    public class TDLevelManager : Singleton<TDLevelManager, TDLevelManager> 
    {
        public delegate void SceneLoadedEvenet(string sceneName);

        public static event SceneLoadedEvenet OnSceneLoaded;

        [SerializeField]
        string LoadingSceneName;

        Transition loadingEffect;

        protected string PrefixLogMessage(string message) => $"Level Manager: {message}";

        private bool transitioning = false;
        string levelToLoadName;
        bool readyToFinalizeLoading;
        Scene unloadingScene;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        EventSystem unloadingSceneEventSystem;

        void DisableSourceSceneEventSystem(Scene sourceScene)
        {
            unloadingSceneEventSystem = this.GetFirstInScene<EventSystem>(sourceScene, sys => sys.gameObject.activeSelf);
            if (unloadingSceneEventSystem != null )
            {
                unloadingSceneEventSystem.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Start a loading transition, supplying needed information later
        /// </summary>
        /// <returns></returns>
        public System.Action<Scene, string, SaveLoader> LoadSceneAsync() 
        {
            if (transitioning)
            {
                Debug.LogError(PrefixLogMessage($"Can't initiate loading since alreay busy loading"));
                return null;
            }

            transitioning = true;
            levelToLoadName = null;
            saveLoader = null;
            levelLoadedStarted = false;

            InitiateLoadingTransition();

            return StartLoading;
        }

        void StartLoading(Scene unloadingScene, string levelToLoadName, SaveLoader saveLoader) {
            this.unloadingScene = unloadingScene;
            this.levelToLoadName = levelToLoadName;
            this.saveLoader = saveLoader;

            DisableSourceSceneEventSystem(unloadingScene);

            Debug.Log(PrefixLogMessage($"Loading '{levelToLoadName}' from '{unloadingScene.name}' with save state: {saveLoader != null}"));
            InitiateLevelSceneLoading();
        }

        private void InitiateLoadingTransition()
        {
            var loadingSceneOperation = SceneManager
                .LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);
            loadingSceneOperation.completed += LoadingSceneOperation_completed;
        }

        private void LoadingSceneOperation_completed(AsyncOperation obj)
        {
            loadingEffect = this.GetFirstInScene<Transition>(LoadingSceneName);

            if (loadingEffect == null)
            {
                Debug.LogError(PrefixLogMessage($"Loading scene '{LoadingSceneName}' does not have any transition effect"));
                transitionPhase = Transition.Phase.Waiting;
            } else
            {
                loadingEffect.OnPhaseChange += LoadingEffect_onPhaseChange;
                loadingEffect.ActivePhase = Transition.Phase.EaseIn;
            }

            if (levelToLoadName != null) InitiateLevelSceneLoading();
        }

        Transition.Phase transitionPhase;

        private void LoadingEffect_onPhaseChange(Transition.Phase phase)
        {
            transitionPhase = phase;

            if (transitionPhase == Transition.Phase.Waiting)
            {
                InitiateLevelSceneLoading();
            } else if (readyToFinalizeLoading && phase == Transition.Phase.Completed) {
                FinalizeTransition();
            }
        }

        bool levelLoadedStarted;

        private void InitiateLevelSceneLoading()
        {
            if (levelLoadedStarted || transitionPhase != Transition.Phase.Waiting)
            {
                Debug.Log(PrefixLogMessage($"Ignoring level load init because started({levelLoadedStarted}) or wrong phase ({transitionPhase})"));
                return;
            }

            levelLoadedStarted = true;
            var levelLoadingOperation = SceneManager
                .LoadSceneAsync(levelToLoadName, LoadSceneMode.Additive);
            levelLoadingOperation.completed += LevelLoadingOperation_completed;
        }


        AudioListener levelSceneAudioListerner;
        /// <summary>
        /// To avoid duplicate audio listeners while transitioning it is disabled
        /// </summary>
        void DisableLevelAudioListener(Scene levelScene)
        {
            levelSceneAudioListerner = this.GetFirstInScene<AudioListener>(levelScene);
            if (levelSceneAudioListerner == null && levelSceneAudioListerner.enabled)
            {
                levelSceneAudioListerner.enabled = false;
            } else
            {
                levelSceneAudioListerner = null;
            }
        }

        SaveLoader saveLoader;

        private void LevelLoadingOperation_completed(AsyncOperation obj)
        {
            var levelScene = SceneManager.GetSceneByName(levelToLoadName);
            SceneManager.SetActiveScene(levelScene);
            DisableLevelAudioListener(levelScene);

            if (saveLoader != null)
            {
                saveLoader();
            } 

            UnloadSourceScene();
        }

        void SwapAudioListeners()
        {
            if (unloadingScene != null)
            {
                var myAudioListener = this.GetFirstInScene<AudioListener>(unloadingScene);

                if (myAudioListener != null)
                {
                    myAudioListener.enabled = false;
                }
            }

            if (levelSceneAudioListerner != null)
            {
                levelSceneAudioListerner.enabled = true;
            }
        }

        private void UnloadSourceScene()
        {
            SwapAudioListeners();

            if (unloadingScene != null)
            {
                var unloadSceneOperation = SceneManager.UnloadSceneAsync(unloadingScene);
                if (loadingEffect == null)
                {
                    unloadSceneOperation.completed += UnloadSceneOperation_completed_withoutEffect; ;
                } else
                {
                    unloadSceneOperation.completed += UnloadSceneOperation_completed_withEffect;
                }

            } else if (loadingEffect != null)
            {
                loadingEffect.ActivePhase = Transition.Phase.EaseOut;
            } else
            {
                FinalizeTransition();
            }
            
        }

        private void UnloadSceneOperation_completed_withEffect(AsyncOperation obj)
        {
            if (transitionPhase == Transition.Phase.Waiting)
            {
                loadingEffect.ActivePhase = Transition.Phase.EaseOut;
            } else
            {
                readyToFinalizeLoading = true;
            }
        }

        private void UnloadSceneOperation_completed_withoutEffect(AsyncOperation obj) => FinalizeTransition();

        private void FinalizeTransition()
        {
            OnSceneLoaded?.Invoke(LoadingSceneName);
            SceneManager.UnloadSceneAsync(LoadingSceneName).completed += TDLevelManager_completed;
        }

        private void TDLevelManager_completed(AsyncOperation obj)
        {
            transitioning = false;
            Debug.Log(PrefixLogMessage($"Transition completed for '{levelToLoadName}'"));
        }
    }
}
