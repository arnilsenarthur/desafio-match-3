#if UNITY_EDITOR
using System;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomEditor(typeof(GameController))]
    public class GameControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "_fallbackDifficultyId");
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

            DrawFallbackDifficultyField();
            DrawPlayModeTools();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFallbackDifficultyField()
        {
            SerializedProperty fallbackProperty = serializedObject.FindProperty("_fallbackDifficultyId");

            if (fallbackProperty == null)
            {
                return;
            }

            SerializedProperty gameConfigProperty = serializedObject.FindProperty("_gameConfig");
            GameConfig gameConfig = gameConfigProperty.objectReferenceValue as GameConfig;
            
            if (gameConfig == null)
            {
                EditorGUILayout.HelpBox("Assign a Game Config to choose a fallback difficulty from its settings.", MessageType.Warning);
                EditorGUILayout.PropertyField(fallbackProperty);
                return;
            }


            GameDifficultySettings[] difficulties = gameConfig.Difficulties;
            if (difficulties == null || difficulties.Length == 0)
            {
                EditorGUILayout.HelpBox("Game Config has no difficulties defined.", MessageType.Warning);
                EditorGUILayout.PropertyField(fallbackProperty);
                return;
            }


            string[] difficultyIds = BuildDifficultyIds(difficulties);
            int selectedIndex = Array.IndexOf(difficultyIds, fallbackProperty.stringValue);

            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            selectedIndex = EditorGUILayout.Popup("Fallback Difficulty Id", selectedIndex, difficultyIds);
            fallbackProperty.stringValue = difficultyIds[selectedIndex];
        }

        private void DrawPlayModeTools()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to restart the game or adjust match time.", MessageType.Info);
                return;
            }

            GameController controller = (GameController)target;
            SerializedProperty fallbackProperty = serializedObject.FindProperty("_fallbackDifficultyId");

            using (new EditorGUI.DisabledScope(!GameService.IsActive))
            {
                if (GUILayout.Button("Restart Game"))
                {
                    serializedObject.ApplyModifiedProperties();
                    controller.RestartGame(fallbackProperty.stringValue);
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("+15 Seconds"))
                {
                    GameService.AdjustTime(15f);
                }

                if (GUILayout.Button("-15 Seconds"))
                {
                    GameService.AdjustTime(-15f);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private static string[] BuildDifficultyIds(GameDifficultySettings[] difficulties)
        {
            string[] ids = new string[difficulties.Length];

            for (int i = 0; i < difficulties.Length; i++)
            {
                ids[i] = difficulties[i].Id;
            }

            return ids;
        }
    }
}

#endif


