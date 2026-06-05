#if UNITY_EDITOR
using Gazeus.DesafioMatch3.Data;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomPropertyDrawer(typeof(TutorialTilePlacement))]
    public sealed class TutorialTilePlacementPropertyDrawer : PropertyDrawer
    {
        private const float OffsetLabelWidth = 44f;
        private const float TypeLabelWidth = 32f;
        private const float AxisFieldWidth = 36f;
        private const float FieldSpacing = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            DrawRow(contentRect, property);

            EditorGUI.EndProperty();
        }

        private static void DrawRow(Rect rect, SerializedProperty property)
        {
            SerializedProperty offset = property.FindPropertyRelative("_offset");
            SerializedProperty typeId = property.FindPropertyRelative("_typeId");
            SerializedProperty offsetX = offset.FindPropertyRelative("x");
            SerializedProperty offsetY = offset.FindPropertyRelative("y");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float x = rect.x;

            Rect offsetLabelRect = new Rect(x, rect.y, OffsetLabelWidth, lineHeight);
            x = offsetLabelRect.xMax;
            Rect offsetXRect = new Rect(x, rect.y, AxisFieldWidth, lineHeight);
            x = offsetXRect.xMax + FieldSpacing;
            Rect offsetYRect = new Rect(x, rect.y, AxisFieldWidth, lineHeight);
            x = offsetYRect.xMax + FieldSpacing * 2f;
            Rect typeLabelRect = new Rect(x, rect.y, TypeLabelWidth, lineHeight);
            x = typeLabelRect.xMax;
            Rect typeRect = new Rect(x, rect.y, rect.xMax - x, lineHeight);

            EditorGUI.LabelField(offsetLabelRect, "Offset", EditorStyles.miniLabel);
            EditorGUI.PropertyField(offsetXRect, offsetX, GUIContent.none);
            EditorGUI.PropertyField(offsetYRect, offsetY, GUIContent.none);
            EditorGUI.LabelField(typeLabelRect, "Type", EditorStyles.miniLabel);
            EditorGUI.PropertyField(typeRect, typeId, GUIContent.none);
        }
    }
}
#endif
