using LMCore.Extensions;
using System;
using System.Xml.Linq;

namespace LMCore.TiledImporter
{
    [Serializable]
    public class TiledTilesetMetadata
    {
        public string Source;
        public int FirstGID;

        public override string ToString() => $"<TilesetMetadata firstGID={FirstGID} source=\"{Source}\" />";

        public static TiledTilesetMetadata From(XElement tileset) => tileset == null ? null :
            new TiledTilesetMetadata() { FirstGID = tileset.GetIntAttribute("firstgid"), Source = tileset.GetAttribute("source") };
    }
}
