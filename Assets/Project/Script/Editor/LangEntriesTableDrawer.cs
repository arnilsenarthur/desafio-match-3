#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    internal static class LangEntriesTableDrawer
    {
        private const float KeyColumnRatio = 0.42f;
        private const float MinKeyColumnWidth = 140f;

        public static void Draw(SerializedProperty entriesProperty, ref string searchFilter, ref Vector2 scroll)
        {
            if (entriesProperty == null)
            {
                EditorGUILayout.HelpBox("Missing entries property.", MessageType.Error);
                return;
            }

            var enabled = ForceEnabled();
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            RestoreEnabled(enabled);

            float keyWidth = Mathf.Max(MinKeyColumnWidth, EditorGUIUtility.currentViewWidth * KeyColumnRatio);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(keyWidth));
            GUILayout.Label("Value", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            enabled = ForceEnabled();
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(240f));

            string filter = searchFilter?.Trim() ?? string.Empty;
            int visibleCount = 0;

            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
                SerializedProperty keyProperty = entryProperty.FindPropertyRelative("Key");
                SerializedProperty valueProperty = entryProperty.FindPropertyRelative("Value");

                if (keyProperty == null || valueProperty == null)
                {
                    continue;
                }

                if (filter.Length > 0 &&
                    keyProperty.stringValue.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    valueProperty.stringValue.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                visibleCount++;
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(keyProperty, GUIContent.none, GUILayout.Width(keyWidth));
                EditorGUILayout.PropertyField(valueProperty, GUIContent.none);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            RestoreEnabled(enabled);

            EditorGUILayout.LabelField(
                filter.Length > 0
                    ? $"Showing {visibleCount} of {entriesProperty.arraySize} entries"
                    : $"{entriesProperty.arraySize} entries",
                EditorStyles.miniLabel);
        }

        private static bool ForceEnabled()
        {
            var enabled = GUI.enabled;
            GUI.enabled = true;
            return enabled;
        }

        private static void RestoreEnabled(bool enabled)
        {
            GUI.enabled = enabled;
        }
    }
}
#endif
