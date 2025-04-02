namespace LMCore.Crawler
{
    public interface IAnchorEffect
    {

        /// <summary>
        /// Called when player is about to enter a tile
        /// </summary>
        public void EnterTile(GridEntity player);

        /// <summary>
        /// Called just as player is leaving a tile
        /// </summary>
        public void ExitTile(GridEntity player);
    }
}
