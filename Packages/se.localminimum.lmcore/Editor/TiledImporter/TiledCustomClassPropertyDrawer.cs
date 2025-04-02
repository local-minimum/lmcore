using LMCore.IO;
using UnityEditor;
using UnityEngine;

namespace LMCore.TiledImporter
{
    [CustomPropertyDrawer(typeof(TiledCustomClass))]
    public class TiledCustomClassPropertyDrawer : PropertyDrawer
    {
        private static float RowGap = 2;
        private static string[] ChildNames = new string[] {
            "Strings",
            "Ints",
            "Floats",
            "Bools",
            "Colors",
            "StringEnums",
            "IntEnums",
        };


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var selfHeight = EditorGUI.GetPropertyHeight(property);

            if (!property.isExpanded) return selfHeight;

            var childrenHeight = 0f;

            foreach (var childName in ChildNames)
            {
                var child = property.FindPropertyRelative(childName);
                if (SerializableDictionarySerializedPropertyUtil.IsEmptySerializableDictionary(child)) continue;
                childrenHeight += EditorGUI.GetPropertyHeight(child, child.isExpanded) + RowGap;
            }

            return selfHeight + RowGap + childrenHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            EditorGUI.BeginProperty(position, label, property);

            var height = EditorGUI.GetPropertyHeight(property);

            var foldOutRect = new Rect(position);
            foldOutRect.height = height;

            property.isExpanded = EditorGUI.Foldout(
                foldOutRect,
                property.isExpanded,
                new GUIContent($"{property.FindPropertyRelative("ClassType").stringValue} [{(property.isExpanded ? "collapse" : "expand")}]"),
                true,
                title
            );

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            var y = position.y + height + RowGap;

            foreach (var childName in ChildNames)
            {
                var child = property.FindPropertyRelative(childName);

                if (SerializableDictionarySerializedPropertyUtil.IsEmptySerializableDictionary(child)) continue;

                var childRect = new Rect(position.x, y, position.width, height);

                var childExpanded = EditorGUI.PropertyField(childRect, child, new GUIContent(childName));

                y += EditorGUI.GetPropertyHeight(child, childExpanded) + RowGap;
            }

            EditorGUI.EndChangeCheck();
        }
    }
}
