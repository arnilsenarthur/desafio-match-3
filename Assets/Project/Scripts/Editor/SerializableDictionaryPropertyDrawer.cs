#if UNITY_EDITOR
using Gazeus.DesafioMatch3.Misc;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomPropertyDrawer(typeof(SerializableDictionary), true)]
    public sealed class SerializableDictionaryPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializableDictionaryDrawer.State state = SerializableDictionaryDrawer.GetOrCreateState(property);
            return SerializableDictionaryDrawer.GetPropertyHeight(property, state);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializableDictionaryDrawer.State state = SerializableDictionaryDrawer.GetOrCreateState(property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect labelRect = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);

            float contentHeight = SerializableDictionaryDrawer.GetDrawHeight(property, state);
            Rect contentRect = new Rect(position.x, labelRect.yMax + spacing, position.width, contentHeight);

            bool enabled = GUI.enabled;
            GUI.enabled = true;
            SerializableDictionaryDrawer.DrawProperty(contentRect, property, state);
            GUI.enabled = enabled;

            EditorGUI.EndProperty();
        }
    }
}
#endif
