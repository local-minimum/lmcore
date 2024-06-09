using UnityEditor;
using LMCore.IO;

namespace TiledImporter {
    [CustomPropertyDrawer(typeof(SerializableDictionary<string, TiledEnum<string>>))]
    public class SerializableStringTiledStringEnumDictionaryDrawer : GenericSerializableDictionaryDrawer<string, TiledEnum<string>> { };

    [CustomPropertyDrawer(typeof(SerializableDictionary<string, TiledEnum<int>>))]
    public class SerializableIntTiledStringEnumDictionaryDrawer : GenericSerializableDictionaryDrawer<string, TiledEnum<int>> { };

    [CustomPropertyDrawer(typeof(SerializableDictionary<string, SerializableDictionary<string, TiledEnum<string>>>))]
    public class SerializableNestedStringTiledStringEnumDictionaryDrawer : GenericSerializableDictionaryDrawer<string, SerializableDictionary<string, TiledEnum<string>>> {};

    [CustomPropertyDrawer(typeof(SerializableDictionary<string, SerializableDictionary<int, TiledEnum<int>>>))]
    public class SerializableNestedIntTiledStringEnumDictionaryDrawer : GenericSerializableDictionaryDrawer<string, SerializableDictionary<int, TiledEnum<int>>> { };

    [CustomPropertyDrawer(typeof(SerializableDictionary<string, TiledCustomClass>))]
    public class SerializableIntTiledCustomClassDictionaryDrawer : GenericSerializableDictionaryDrawer<string, TiledCustomClass> { };
}
