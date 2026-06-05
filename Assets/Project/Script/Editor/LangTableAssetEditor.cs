#if UNITY_EDITOR
using Gazeus.DesafioMatch3.Localization;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomEditor(typeof(LangTableAsset))]
    public class LangTableAssetEditor : UnityEditor.Editor
    {
        private string _searchFilter = string.Empty;
        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            var table = (LangTableAsset)target;

            EditorGUILayout.LabelField("Locale", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Language Id", table.name);

            string assetPath = AssetDatabase.GetAssetPath(table);
            if (!string.IsNullOrEmpty(assetPath))
            {
                EditorGUILayout.LabelField("Source", assetPath, EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4f);

            SerializedProperty entriesProperty = serializedObject.FindProperty("_entries");
            serializedObject.Update();
            LangEntriesTableDrawer.Draw(entriesProperty, ref _searchFilter, ref _scroll);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
