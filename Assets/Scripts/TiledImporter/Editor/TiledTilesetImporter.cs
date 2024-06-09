using System.Xml.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TiledImporter
{
    [ScriptedImporter(1, new[] { "tsx" })]
    public class TiledTilesetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            XDocument doc = XDocument.Load(assetPath);
            var tileset = ProcessTileset(doc.Element("tileset"));
            Debug.Log($"{tileset.Name} - imported");

            ctx.AddObjectToAsset(tileset.Name, tileset);
        }

        public TiledTileset ProcessTileset(XElement tileset)
        {
            var enums = new TiledEnums();
            return TiledTileset.From(tileset, enums, assetPath);
        }
    }
}
