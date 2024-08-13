using UnityEngine;

namespace LMCore.IO
{
    /// <summary>
    /// Useful metadata about the game to include in a save
    /// </summary>
    [System.Serializable]   
    public class GameEnvironment
    {
        public string buildGUID;
        public string version;
        public string unityVersion;
        public string systemLanguage;
        public string platform;
        public bool genuine;
        public float sessionPlayTime;

        public static GameEnvironment FromApplication() {
            return new GameEnvironment()
            {
                buildGUID = Application.buildGUID,
                version = Application.version,
                unityVersion = Application.unityVersion,
                systemLanguage = Application.systemLanguage.ToString(),
                platform = Application.platform.ToString(),
                genuine = Application.genuine,
                sessionPlayTime = Time.realtimeSinceStartup,
            };
        }
    }
}
