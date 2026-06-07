#if UNITY_EDITOR
using System.Collections.Generic;
using Gazeus.DesafioMatch3.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomEditor(typeof(Label))]
    public class LabelEditor : UnityEditor.Editor
    {
        private static string _previewLanguageId = LanguageCodes.EnUs;

        private static string[] _keyOptions;

        public override void OnInspectorGUI()
        {
            var label = (Label)target;
            TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                EditorGUILayout.HelpBox("Label requires a TextMeshPro component.", MessageType.Error);
                return;
            }

            SerializedObject textSerialized = new(text);
            textSerialized.Update();
            SerializedProperty textProperty = textSerialized.FindProperty("m_text");

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play mode uses TextMeshPro text as the localization key.", MessageType.None);
                if (textProperty != null)
                {
                    EditorGUILayout.PropertyField(textProperty, new GUIContent("Key (Text)"));
                }

                textSerialized.ApplyModifiedProperties();
                return;
            }

            DrawEditorTools(text, textProperty);
            textSerialized.ApplyModifiedProperties();
        }

        private void DrawEditorTools(TMP_Text text, SerializedProperty textProperty)
        {
            LocalizationService.Initialize();

            string currentKey = text.text?.Trim() ?? string.Empty;

            BuildKeyOptions();
            int selectedKeyIndex = IndexOfKey(currentKey);
            int newKeyIndex = DrawPopup("Localization Key", selectedKeyIndex, _keyOptions);

            if (newKeyIndex != selectedKeyIndex)
            {
                string newKey = newKeyIndex > 0 ? _keyOptions[newKeyIndex] : string.Empty;
                ApplyKeyToText(text, textProperty, newKey);
                currentKey = newKey;
            }

            if (selectedKeyIndex == 0 && !string.IsNullOrEmpty(currentKey))
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Custom Key", currentKey);
                EditorGUI.EndDisabledGroup();
            }

            DrawPreviewLanguagePopup();

            LocalizationService.SetLanguage(_previewLanguageId);
            string preview = LocalizationService.Localize(currentKey);

            EditorGUILayout.LabelField($"Preview ({_previewLanguageId})", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(preview, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3f));
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawPreviewLanguagePopup()
        {
            IReadOnlyList<string> languageIds = LocalizationService.GetLanguageIds();
            if (languageIds.Count == 0)
            {
                EditorGUILayout.HelpBox("No locale tables found under Resources/Localization.", MessageType.Warning);
                return;
            }

            if (IndexOfLanguage(languageIds, _previewLanguageId) < 0)
            {
                _previewLanguageId = languageIds[0];
            }

            int currentIndex = IndexOfLanguage(languageIds, _previewLanguageId);
            var options = new string[languageIds.Count];
            for (int i = 0; i < languageIds.Count; i++)
            {
                options[i] = languageIds[i];
            }

            int newIndex = DrawPopup("Preview Language", currentIndex, options);
            if (newIndex != currentIndex)
            {
                _previewLanguageId = languageIds[newIndex];
            }
        }

        private static int DrawPopup(string label, int selectedIndex, string[] options)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            int index = EditorGUILayout.Popup(selectedIndex, options);
            EditorGUILayout.EndHorizontal();
            return index;
        }

        private static void ApplyKeyToText(TMP_Text text, SerializedProperty textProperty, string key)
        {
            Undo.RecordObject(text, "Set Localization Key");

            if (textProperty != null)
            {
                textProperty.stringValue = key;
            }
            else
            {
                text.text = key;
            }

            EditorUtility.SetDirty(text);
        }

        private static void BuildKeyOptions()
        {
            IReadOnlyList<string> keys = LocalizationService.GetKeys();
            int count = keys.Count + 1;
            _keyOptions = new string[count];
            _keyOptions[0] = "(none)";

            for (int i = 0; i < keys.Count; i++)
            {
                _keyOptions[i + 1] = keys[i];
            }
        }

        private static int IndexOfKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return 0;
            }

            IReadOnlyList<string> keys = LocalizationService.GetKeys();
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == key)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private static int IndexOfLanguage(IReadOnlyList<string> languageIds, string languageId)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                return -1;
            }

            for (int i = 0; i < languageIds.Count; i++)
            {
                if (languageIds[i] == languageId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
#endif
