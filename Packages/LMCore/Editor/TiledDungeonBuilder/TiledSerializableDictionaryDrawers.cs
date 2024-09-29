using LMCore.IO;
using UnityEditor;
using LMCore.TiledDungeon.Style;

namespace LMCore.TiledDungeon
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<string, DungeonStyle>))]
    public class SerializableAbsDungeonStyleDictionaryDrawer : GenericSerializableDictionaryDrawer<string, DungeonStyle> { };
}
