using UnityEditor;
using UnityEngine;
using LMCore.IO;

namespace LMCore.TiledImporter
{
    [CustomPropertyDrawer(typeof(TiledCustomProperties))]
    public class TiledCustomPropertiesPropertyDrawer : PropertyDrawer
    {
        private static float RowGap = 2;
        private static float SelfHeight => 22;

        private static string[] ChildNames = new string[] { 
            "Strings",
            "Ints",
            "Floats",
            "Bools",
            "Colors",
            "StringEnums",
            "IntEnums",
            "Classes",
        };


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return SelfHeight;

            var childrenHeight = 0f;

            foreach (var childName in ChildNames)
            {
                var child = property.FindPropertyRelative(childName);
                if (SerializableDictionarySerializedPropertyUtil.IsEmptySerializableDictionary(child) ) continue;
                childrenHeight += EditorGUI.GetPropertyHeight(child, child.isExpanded) + RowGap;
            }

            return SelfHeight + RowGap + childrenHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            EditorGUI.BeginProperty(position, label, property);

            var height = SelfHeight;
            var foldOutRect = new Rect(position);
            foldOutRect.height = height;

            property.isExpanded = EditorGUI.Foldout(
                foldOutRect, 
                property.isExpanded, 
                new GUIContent($"Custom [{(property.isExpanded ? "collapse" : "expand")}]"), 
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

                if (SerializableDictionarySerializedPropertyUtil.IsEmptySerializableDictionary(child) ) continue;
                
                var childRect = new Rect(position.x, y, position.width, height);

                var childExpanded = EditorGUI.PropertyField(childRect, child, new GUIContent(childName));

                y += EditorGUI.GetPropertyHeight(child, childExpanded) + RowGap;
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndChangeCheck();
        }

    }
}
