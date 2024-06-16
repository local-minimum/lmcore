using TiledImporter;
using System;

namespace TiledDungeon
{
    [Serializable]
    public class TileModification
    {
        public string Layer;
        public TiledCustomProperties LayerProperties;
        public TiledTile Tile;
    }

}
