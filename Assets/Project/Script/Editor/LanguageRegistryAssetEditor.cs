#if UNITY_EDITOR
using Gazeus.DesafioMatch3.Localization;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomEditor(typeof(LanguageRegistryAsset))]
    public class LanguageRegistryAssetEditor : UnityEditor.Editor
    {
        private string _searchFilter = string.Empty;
        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Language Registry", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Maps language ids (EnUs, PtBr) to native display names shown in the UI.",
                MessageType.None);

            SerializedProperty entriesProperty = serializedObject.FindProperty("_entries");
            serializedObject.Update();
            LangEntriesTableDrawer.Draw(entriesProperty, ref _searchFilter, ref _scroll);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
