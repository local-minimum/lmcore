using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace LMCore.TiledImporter
{
    [CustomPropertyDrawer(typeof(TiledEnum<string>))]
    public class TiledStringStringPropertyDrawer : GenericTiledEnumPropertyDrawer<string> { }

    [CustomPropertyDrawer(typeof(TiledEnum<int>))]
    public class TiledStringIntPropertyDrawer : GenericTiledEnumPropertyDrawer<int> { }

    public class GenericTiledEnumPropertyDrawer<T> : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.LabelField(position, property.GetUnderlyingValue().ToString());
        }
    }
}
