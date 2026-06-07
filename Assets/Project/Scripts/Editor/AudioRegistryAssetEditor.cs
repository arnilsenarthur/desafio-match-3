#if UNITY_EDITOR
using Gazeus.DesafioMatch3.Audio;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomEditor(typeof(AudioRegistryAsset))]
    public sealed class AudioRegistryAssetEditor : UnityEditor.Editor
    {
        private readonly SerializableDictionaryDrawer.State _entriesState = new();

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Audio Registry", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Maps audio keys to clips. Use Desafio Match 3 > Audio > Rebuild Audio Registry to refresh from Project/Audio.",
                MessageType.None);

            SerializedProperty entriesProperty = serializedObject.FindProperty("_entries");
            serializedObject.Update();
            SerializableDictionaryDrawer.Draw(
                entriesProperty,
                _entriesState,
                new SerializableDictionaryDrawer.Options
                {
                    KeyColumnLabel = "Audio Key",
                    ValueColumnLabel = "Sound Entry",
                    MaxScrollHeight = 360f,
                });
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
