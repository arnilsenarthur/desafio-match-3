#if UNITY_EDITOR
using Gazeus.DesafioMatch3.UI;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomEditor(typeof(UIButton), true)]
    [CanEditMultipleObjects]
    public class UIButtonEditor : ButtonEditor
    {
        private SerializedProperty _defaultSprite;
        private SerializedProperty _pressedSprite;
        private SerializedProperty _pressedOffset;

        protected override void OnEnable()
        {
            base.OnEnable();

            _defaultSprite = serializedObject.FindProperty("_defaultSprite");
            _pressedSprite = serializedObject.FindProperty("_pressedSprite");
            _pressedOffset = serializedObject.FindProperty("_pressedOffset");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Button Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_defaultSprite);
            EditorGUILayout.PropertyField(_pressedSprite);
            EditorGUILayout.PropertyField(_pressedOffset);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
