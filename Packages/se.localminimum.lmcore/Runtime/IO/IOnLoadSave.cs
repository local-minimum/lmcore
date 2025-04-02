namespace LMCore.IO
{
    public interface IOnLoadSave
    {
        /// <summary>
        /// Called once a game state has been loaded
        /// </summary>
        public void OnLoad<T>(T save) where T : new();

        /// <summary>
        /// Ordering of being called after game state has been loaded, higher value gets called earlier
        /// </summary>
        public int OnLoadPriority { get; }
    }
}
