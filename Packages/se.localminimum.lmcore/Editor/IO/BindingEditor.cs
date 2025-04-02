using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.IO
{
    [CustomEditor(typeof(Binding))]
    public class BindingEditor : Editor
    {
        static readonly int smallButtonWidth = 30;
        static readonly int smallLabelWidth = 90;

        SerializedProperty inputAssetProp;
        SerializedProperty mapsProp;
        SerializedProperty actionsProp;
        SerializedProperty actionNamesProp;

        int newMapIndex;

        private void OnEnable()
        {
            inputAssetProp = serializedObject.FindProperty("inputAsset");
            mapsProp = serializedObject.FindProperty("maps");
            actionsProp = serializedObject.FindProperty("actions");
            actionNamesProp = serializedObject.FindProperty("actionNames");
        }

        static IEnumerable<KeyValuePair<string, string>> ListDictonaryContent(SerializedProperty prop)
        {
            if (prop == null)
            {
                yield break;
            }

            var values = prop.FindPropertyRelative("values");
            var keys = prop.FindPropertyRelative("keys");

            if (values == null || keys == null || (!values.isArray && !keys.isArray))
            {
                yield break;
            }

            for (int i = 0, l = Mathf.Min(values.arraySize, keys.arraySize); i < l; i++)
            {
                var key = keys.GetArrayElementAtIndex(i);
                var value = values.GetArrayElementAtIndex(i);

                yield return new KeyValuePair<string, string>(key.stringValue, value.stringValue);
            }
        }

        static void UpdateDictionary(SerializedProperty prop, KeyValuePair<string, string> kvp)
        {
            var values = prop.FindPropertyRelative("values");
            var keys = prop.FindPropertyRelative("keys");

            if (values == null || keys == null || (!values.isArray && !keys.isArray))
            {
                Debug.LogError($"{prop} is not a serializable dictionary prop");
                return;
            }

            SerializedProperty key;
            SerializedProperty value;
            for (int i = 0, l = Mathf.Min(values.arraySize, keys.arraySize); i < l; i++)
            {
                key = keys.GetArrayElementAtIndex(i);
                value = values.GetArrayElementAtIndex(i);

                if (key.stringValue == kvp.Key)
                {
                    value.stringValue = kvp.Value;
                    return;
                }
            }

            keys.arraySize++;
            values.arraySize++;

            key = keys.GetArrayElementAtIndex(keys.arraySize - 1);
            value = values.GetArrayElementAtIndex(values.arraySize - 1);

            key.stringValue = kvp.Key;
            value.stringValue = kvp.Value;
        }

        static void RemoveKey(SerializedProperty prop, string key)
        {
            var values = prop.FindPropertyRelative("values");
            var keys = prop.FindPropertyRelative("keys");

            if (values == null || keys == null || (!values.isArray && !keys.isArray))
            {
                Debug.LogError($"{prop} is not a serializable dictionary prop");
                return;
            }

            bool found = false;

            SerializedProperty prevKey = null;
            SerializedProperty prevValue = null;
            for (int i = 0, l = Mathf.Min(values.arraySize, keys.arraySize); i < l; i++)
            {
                SerializedProperty currentKey = keys.GetArrayElementAtIndex(i);
                SerializedProperty currentValue = values.GetArrayElementAtIndex(i);

                if (currentKey.stringValue == key)
                {
                    found = true;
                }
                else if (found)
                {
                    prevKey.stringValue = currentKey.stringValue;
                    prevValue.stringValue = currentValue.stringValue;
                }

                prevKey = currentKey;
                prevValue = currentValue;
            }

            if (found)
            {
                keys.arraySize--;
                values.arraySize--;
            }
            else
            {
                Debug.LogWarning($"{prop} doesn't have a key '{key}'");
            }

        }

        Dictionary<string, int> addActionSelectedOption = new Dictionary<string, int>();

        void DrawMap(InputActionAsset asset, KeyValuePair<string, string> mapSetting)
        {
            var assetMap = asset.actionMaps.FirstOrDefault(m => m.id.ToString() == mapSetting.Key);
            if (assetMap == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{mapSetting.Value} ({mapSetting.Key})", EditorStyles.boldLabel);
                if (GUILayout.Button("-"))
                {
                    RemoveKey(mapsProp, mapSetting.Key);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox($"Configured action map '{mapSetting.Value}' missing in asset!", MessageType.Error);
                return;
            }

            if (assetMap.name != mapSetting.Value)
            {
                UpdateDictionary(mapsProp, new KeyValuePair<string, string>(mapSetting.Key, assetMap.name));
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(mapSetting.Value, EditorStyles.boldLabel);
            if (GUILayout.Button("-", GUILayout.Width(smallButtonWidth)))
            {
                RemoveKey(mapsProp, mapSetting.Key);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            var configuredActions = ListDictonaryContent(actionsProp)
                .Where(kvp => kvp.Value == assetMap.id.ToString())
                .ToList();

            var configuredActionNames = ListDictonaryContent(actionNamesProp)
                .Where(kvp => configuredActions.Any(ca => ca.Key == kvp.Key))
                .ToList();

            foreach (var ca in configuredActions)
            {
                var action = assetMap.actions.FirstOrDefault(a => a.id.ToString() == ca.Key);

                bool findsName = configuredActionNames.Any(can => can.Key == ca.Key);
                var nameConf = findsName ?
                    configuredActionNames.First(can => can.Key == ca.Key) :
                    new KeyValuePair<string, string>(ca.Key, action == null ? "[UNKNOWN]" : action.name);

                if (action == null)
                {

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{nameConf.Value} ({nameConf.Key})", EditorStyles.helpBox);
                    if (GUILayout.Button("-", GUILayout.Width(smallButtonWidth)))
                    {
                        RemoveKey(actionsProp, nameConf.Key);
                        RemoveKey(actionNamesProp, nameConf.Key);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.HelpBox($"Configured action '{nameConf.Value}' missing in '{assetMap.name}' action map!", MessageType.Error);
                    continue;
                }


                if (nameConf.Value != action.name)
                {
                    nameConf = new KeyValuePair<string, string>(nameConf.Key, action.name);
                    findsName = false;
                }

                if (!findsName)
                {
                    UpdateDictionary(actionNamesProp, nameConf);
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(nameConf.Value, EditorStyles.helpBox);
                if (GUILayout.Button("-", GUILayout.Width(smallButtonWidth)))
                {
                    RemoveKey(actionsProp, nameConf.Key);
                    RemoveKey(actionNamesProp, nameConf.Key);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (configuredActions.Count == 0)
            {
                EditorGUILayout.HelpBox($"Action map '{mapSetting.Value}' has no bindings selected! Please add at least one.", MessageType.Warning);
            }

            var unused = assetMap.actions
                .Where(a => !configuredActions.Any(ca => ca.Key == a.id.ToString()))
                .ToList();

            if (unused.Count > 0)
            {
                var currentSelected = Mathf.Min(
                    addActionSelectedOption.GetValueOrDefault(assetMap.id.ToString(), 0),
                    unused.Count - 1);

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Add Action:", GUILayout.Width(smallLabelWidth));

                addActionSelectedOption[assetMap.id.ToString()] = EditorGUILayout.Popup(
                    currentSelected,
                    unused.Select(a => a.name).ToArray()
                );

                if (GUILayout.Button("+", GUILayout.Width(smallButtonWidth)) && unused.Count > currentSelected)
                {
                    var newAction = unused[currentSelected];
                    UpdateDictionary(actionsProp, new KeyValuePair<string, string>(newAction.id.ToString(), assetMap.id.ToString()));
                    UpdateDictionary(actionNamesProp, new KeyValuePair<string, string>(newAction.id.ToString(), newAction.name));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(inputAssetProp);

            if (inputAssetProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Must have an input action asset to work!", MessageType.Info);
                return;
            }

            InputActionAsset asset = inputAssetProp.objectReferenceValue as InputActionAsset;
            var configuredMaps = ListDictonaryContent(mapsProp).ToList();

            foreach (var map in configuredMaps)
            {
                DrawMap(asset, map);
            }

            if (configuredMaps.Count == 0)
            {
                EditorGUILayout.HelpBox($"No action map selected! Please select at least one.", MessageType.Warning);
            }

            var unused = asset.actionMaps
                    .Where(m => !configuredMaps.Any(kvp => kvp.Key == m.id.ToString()))
                    .ToList();

            if (unused.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Add Map:", GUILayout.Width(smallLabelWidth));

                newMapIndex = EditorGUILayout.Popup(newMapIndex, unused.Select(m => m.name).ToArray());

                if (GUILayout.Button("+", GUILayout.Width(smallButtonWidth)))
                {
                    var map = unused[newMapIndex];
                    UpdateDictionary(mapsProp, new KeyValuePair<string, string>(map.id.ToString(), map.name));
                }

                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
