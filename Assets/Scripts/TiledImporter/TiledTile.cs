using System;
using System.Xml.Linq;
using LMCore.Extensions;

namespace TiledImporter
{
    [Serializable]
    public class TiledTile
    {
        public string Type;
        public int Id; 
        public TiledCustomProperties CustomProperties;

        public static Func<XElement, TiledTile> FromFactory(TiledEnums enums)
        {
            return (XElement tile) => From(tile, enums);
        }

        public static TiledTile From(XElement tile, TiledEnums enums)
        {
            if (tile == null) return null;

            return new TiledTile()
            {
                Type = tile.GetAttribute("type"),
                Id = tile.GetIntAttribute("id"),
                CustomProperties = TiledCustomProperties.From(tile.Element("properties"), enums),
            };
        }
    }
}
