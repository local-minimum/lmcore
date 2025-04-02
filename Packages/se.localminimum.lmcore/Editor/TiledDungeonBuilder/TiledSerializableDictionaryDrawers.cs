using LMCore.IO;
using LMCore.TiledDungeon.Style;
using UnityEditor;

namespace LMCore.TiledDungeon
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<string, DungeonStyle>))]
    public class SerializableAbsDungeonStyleDictionaryDrawer : GenericSerializableDictionaryDrawer<string, DungeonStyle> { };
}
