using LMCore.IO;
using LMCore.Extensions;
using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableDictionary<string, float>))]
public class SerializableStringFloatDictionaryDrawer : GenericSerializableDictionaryDrawer<string, float> {};

[CustomPropertyDrawer(typeof(SerializableDictionary<string, bool>))]
public class SerializableStringBoolDictionaryDrawer : GenericSerializableDictionaryDrawer<string, bool> {};

[CustomPropertyDrawer(typeof(SerializableDictionary<string, string>))]
public class SerializableStringStringDictionaryDrawer : GenericSerializableDictionaryDrawer<string, string> {};

[CustomPropertyDrawer(typeof(SerializableDictionary<string, int>))]
public class SerializableStringIntDictionaryDrawer : GenericSerializableDictionaryDrawer<string, int> {};

[CustomPropertyDrawer(typeof(SerializableDictionary<string, GameObject>))]
public class SerializableStringGODictionaryDrawer : GenericSerializableDictionaryDrawer<string, GameObject> {};

[CustomPropertyDrawer(typeof(SerializableDictionary<string, Color>))]
public class SerializableColorFloatDictionaryDrawer : GenericSerializableDictionaryDrawer<string, Color> {};

public class GenericSerializableDictionaryDrawer<TKey, TValue> : PropertyDrawer 
{
    private static float RowGap = 2;
    private static float RowItemGap = 2;

    protected TKey addKey;
    protected TValue addValue;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var keys = property.FindPropertyRelative("keys");
        var h = EditorGUI.GetPropertyHeight(property);
        
        if (!property.isExpanded) return EditorGUI.GetPropertyHeight(property);

        // +1 Title and Sometimes +1 Add new item row
        var n = ValueTypeWithKnownDrawer() ? keys.arraySize + 2 : keys.arraySize + 1;

        return h * n + RowGap * (n - 1);
    }

    bool ValueTypeWithKnownDrawer()
    {
        var t = typeof(TValue);

        return t == typeof(string)
            || t == typeof(int)
            || t == typeof(bool)
            || t == typeof(float)
            || t == typeof(Rect)
            || t == typeof(RectInt)
            || t == typeof(Vector2)
            || t == typeof(Vector2Int)
            || t == typeof(Vector3)
            || t == typeof(Vector3Int)
            || t == typeof(Color)
            || t == typeof(UnityEngine.Object);
    }

    bool KeyPropertyEquals(SerializedProperty property, TKey value)
    {
        var t = typeof(TKey); // value == null ? Nullable.GetUnderlyingType(typeof(TKey)) : value.GetType(); //  Nullable.GetUnderlyingType(value.GetType());

        if (t == typeof(string))
        {
            return property.stringValue == Convert.ToString(value);
        }
        else if (t == typeof(int))
        {
            return property.intValue == Convert.ToInt32(value);
        }
        else if (t == typeof(bool))
        {
            return property.boolValue == Convert.ToBoolean(value);
        }
        else if (t == typeof(float))
        {
            return property.floatValue == (float)Convert.ChangeType(value, typeof(float));
        }
        else if (t == typeof(Rect))
        {
            return property.rectValue == (Rect)Convert.ChangeType(value, typeof(Rect));
        }
        else if (t == typeof(RectInt))
        {
            var pVal = property.rectValue;
            var v = (RectInt)Convert.ChangeType(value, typeof(RectInt));
            return pVal.min == v.min && pVal.size == v.size;
        }
        else if (t == typeof(Vector2))
        {
            return property.vector2Value == (Vector2)Convert.ChangeType(value, typeof(Vector2));
        }
        else if (t == typeof(Vector2Int))
        {
            return property.vector2IntValue == (Vector2Int)Convert.ChangeType(value, typeof(Vector2Int));
        }
        else if (t == typeof(Vector3))
        {
            return property.vector3Value == (Vector3)Convert.ChangeType(value, typeof(Vector3));
        }
        else if (t == typeof(Vector3Int))
        {
            return property.vector3IntValue == (Vector3Int)Convert.ChangeType(value, typeof(Vector3Int));
        }
        else if (t == typeof(Color))
        {
            return property.colorValue == (Color)Convert.ChangeType(value, typeof(Color));
        }
        else if (t == typeof(UnityEngine.Object))
        {
            return property.objectReferenceValue == (UnityEngine.Object)Convert.ChangeType(value, typeof(UnityEngine.Object));
        } else if (t == typeof(object))
        {
            // TODO: It's uncertain how this should be done..
            return true; 
        }

        throw new NotImplementedException($"Don't know how to compare {(value == null ? "NULL" : value)} ({t} / {value?.GetType()})");
    }

    void AssignPropertyValue<T>(SerializedProperty property, T value)
    {
        var t = typeof(T);

        if (t == typeof(string))
        {
            property.stringValue = Convert.ToString(value);
            return;
        } else if (t.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            var o = (UnityEngine.Object)Convert.ChangeType(value, t);
            property.objectReferenceValue = o;
            return;
        }


        switch (value)
        {
            case int v when t == typeof(int):
                property.intValue = v;
                break;

            case bool v when t == typeof(bool):
                property.boolValue = v;
                break;

            case float v when t == typeof(float):
                property.floatValue = v;
                break;

            case Rect v when t == typeof(Rect):
                property.rectValue = v;
                break;

            case RectInt v when t == typeof(RectInt):
                property.rectIntValue = v;
                break;

            case Vector2 v when t == typeof(Vector2):
                property.vector2Value = v;
                break;

            case Vector2Int v when t == typeof(Vector2Int):
                property.vector2IntValue = v;
                break;

            case Vector3 v when t == typeof(Vector3):
                property.vector3Value = v;
                break;

            case Vector3Int v when t == typeof(Vector3Int):
                property.vector3IntValue = v;
                break;

            case Color v when t == typeof(Color):
                property.colorValue = v;
                break;

            case object when t == typeof(object):
                Debug.LogWarning("Can't handle assigning random objects");
                break;

            default:
                throw new NotImplementedException($"Don't know how to store {value} ({value?.GetType()})");
        }
    }

    T DrawInputField<T>(Rect rect, T value)
    {
        var t = typeof(T);

        if (t == typeof(string))
        {
            return (T)Convert.ChangeType(EditorGUI.TextField(rect, Convert.ToString(value)), typeof(T));
        } else if (t.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            var o = (UnityEngine.Object)Convert.ChangeType(value, t);
            return (T)Convert.ChangeType(EditorGUI.ObjectField(rect, "", o, t, true), typeof(T));
        }

        switch (value)
        {
            case int v when t == typeof(int):
                return (T) Convert.ChangeType(EditorGUI.IntField(rect, v), typeof(T));
            case float v when t == typeof(float):
                return (T) Convert.ChangeType(EditorGUI.FloatField(rect, v), typeof(T));
            case bool v when t == typeof(bool):
                return (T) Convert.ChangeType(EditorGUI.Toggle(rect, v), typeof(T));

            case Rect v when t == typeof(Rect):
                return (T) Convert.ChangeType(EditorGUI.RectField(rect, v), typeof(T));
            case RectInt v when t == typeof(RectInt):
                return (T) Convert.ChangeType(EditorGUI.RectIntField(rect, v), typeof(T));

            case Vector2 v when t == typeof(Vector2):
                return (T) Convert.ChangeType(EditorGUI.Vector2Field(rect, "", v), typeof(T));
            case Vector2Int v when t == typeof(Vector2Int):
                return (T) Convert.ChangeType(EditorGUI.Vector2IntField(rect, "", v), typeof(T));

            case Vector3 v when t == typeof(Vector3):
                return (T) Convert.ChangeType(EditorGUI.Vector3Field(rect, "", v), typeof(T));
            case Vector3Int v when t == typeof(Vector3Int):
                return (T) Convert.ChangeType(EditorGUI.Vector3IntField(rect, "", v), typeof(T));

            case Color v when t == typeof(Color):
                return (T) Convert.ChangeType(EditorGUI.ColorField(rect, v), typeof(T));

            default:
                Debug.Log(value);
                EditorGUI.LabelField(rect, value?.ToString().Truncate(50) ?? "");
                return value;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUIStyle arrowLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

        GUIContent keyLabelContent = new GUIContent("Add Key:");
        GUIContent valueLabelContent = new GUIContent("Value:");
        GUIContent addButtonContent = new GUIContent("+");
        GUIContent removeButtonContent = new GUIContent("-");
        GUIContent arrowLabelContent = new GUIContent("->");

        EditorGUI.BeginProperty(position, label, property);
        var height = EditorGUI.GetPropertyHeight(property);
        var keys = property.FindPropertyRelative("keys");
        var values = property.FindPropertyRelative("values");

        var foldOutRect = new Rect(position);
        foldOutRect.height = height;

        property.isExpanded = EditorGUI.Foldout(
            foldOutRect, 
            property.isExpanded, 
            new GUIContent(property.isExpanded ? property.displayName: $"{property.displayName} [{keys.arraySize} entries]"), 
            true
        );

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }
        var valueHasKnownDrawer = ValueTypeWithKnownDrawer();

        var indent = EditorGUI.indentLevel;

        EditorGUI.indentLevel = 0;
        var removeButtonWidth = EditorStyles.miniButton.CalcSize(removeButtonContent).x;
        var middleArrowWidth = EditorStyles.label.CalcSize(arrowLabelContent).x;

        var split = Mathf.Floor((position.width - middleArrowWidth - removeButtonWidth - 3 * RowItemGap) / 2);

        bool addKeyUnique = true;

        var y = position.y + height + RowGap;
        for (var i = 0; i < keys.arraySize; i++) { 
            var keyRect = new Rect(position.x, y, split, height);
            var arrowRect = new Rect(keyRect.xMax, y, middleArrowWidth, height);
            var valueRect = new Rect(arrowRect.xMax + RowItemGap, y, split, height);
            var removeButtonRect = new Rect(valueRect.xMax + RowItemGap, y, removeButtonWidth, height);

            var key = keys.GetArrayElementAtIndex(i);

            if (addKeyUnique)
            {
                addKeyUnique = !KeyPropertyEquals(key, addKey);
            }

            EditorGUI.LabelField(keyRect, key.stringValue);
            EditorGUI.LabelField(arrowRect, arrowLabelContent, arrowLabelStyle);
            if (valueHasKnownDrawer)
            {
                EditorGUI.PropertyField(valueRect, values.GetArrayElementAtIndex(i), GUIContent.none);
            } else
            {
                //TODO: This does not actually print out the value of the serialized object at the array index
                // See https://gist.github.com/douduck08/6d3e323b538a741466de00c30aa4b61f
                EditorGUI.LabelField(valueRect, values.GetArrayElementAtIndex(i)?.serializedObject.targetObject.ToString().Truncate(50) ?? "[null]");
            }

            if (GUI.Button(removeButtonRect, removeButtonContent))
            {
                keys.DeleteArrayElementAtIndex(i);
                values.DeleteArrayElementAtIndex(i);
            }
            y += height + RowGap;
        }

        if (valueHasKnownDrawer)
        {
            var keyLabelRect = new Rect(position.x, y, EditorStyles.label.CalcSize(keyLabelContent).x, height);
            EditorGUI.LabelField(keyLabelRect, keyLabelContent);

            var valueLabelWidth = EditorStyles.label.CalcSize(valueLabelContent).x;

            var addFieldWidth = (position.width - keyLabelRect.width - valueLabelWidth - removeButtonWidth - 4 * RowItemGap) / 2;
            var addKeyRect = new Rect(keyLabelRect.xMax + RowItemGap, y, addFieldWidth, height);
            addKey = DrawInputField(addKeyRect, addKey);

            var valueLabelRect = new Rect(addKeyRect.xMax + RowItemGap, y, valueLabelWidth, height);
            EditorGUI.LabelField(valueLabelRect, valueLabelContent);

            var addValueRect = new Rect(valueLabelRect.xMax + RowItemGap, y, addFieldWidth, height);
            addValue = DrawInputField(addValueRect, addValue);

            var addButtonRect = new Rect(addValueRect.xMax + RowItemGap, y, EditorStyles.miniButton.CalcSize(addButtonContent).x, height);
            GUI.enabled = addKeyUnique;
            if (GUI.Button(addButtonRect, addButtonContent))
            {
                keys.arraySize++;
                values.arraySize++;

                AssignPropertyValue(keys.GetArrayElementAtIndex(keys.arraySize - 1), addKey);
                AssignPropertyValue(values.GetArrayElementAtIndex(values.arraySize - 1), addValue);


                addKey = default;
                addValue = default;
            }
            GUI.enabled = true;
        }

        EditorGUI.indentLevel = indent;

        if (EditorGUI.EndChangeCheck())
        {
            property.serializedObject.ApplyModifiedProperties();
        }

        EditorGUI.EndProperty();
    }
}
