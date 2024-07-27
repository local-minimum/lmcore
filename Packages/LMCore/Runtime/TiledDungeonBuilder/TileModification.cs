using LMCore.TiledImporter;
using System;

namespace LMCore.TiledDungeon
{
    [Serializable]
    public class TileModification
    {
        public string Layer;
        public TiledCustomProperties LayerProperties;
        public TiledTile Tile;
    }

}
