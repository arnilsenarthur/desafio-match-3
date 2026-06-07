#if UNITY_EDITOR
using Gazeus.DesafioMatch3.Audio;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomPropertyDrawer(typeof(AudioRegistryEntry))]
    public sealed class AudioRegistryEntryPropertyDrawer : PropertyDrawer
    {
        private const float ClipLeftSpacing = 8f;
        private const float Spacing = 4f;
        private const float MusicWidth = 78f;
        private const float VolumeWidth = 108f;
        private const float MusicLabelWidth = 38f;
        private const float VolumeLabelWidth = 46f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty clipProperty = property.FindPropertyRelative("_clip");
            SerializedProperty isMusicProperty = property.FindPropertyRelative("_isMusic");
            SerializedProperty volumeProperty = property.FindPropertyRelative("_volume");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float clipWidth = position.width - ClipLeftSpacing - MusicWidth - VolumeWidth - Spacing * 2f;

            Rect clipRect = new Rect(position.x + ClipLeftSpacing, position.y, clipWidth, lineHeight);
            Rect musicRect = new Rect(clipRect.xMax + Spacing, position.y, MusicWidth, lineHeight);
            Rect volumeRect = new Rect(musicRect.xMax + Spacing, position.y, VolumeWidth, lineHeight);

            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;
            EditorGUI.PropertyField(clipRect, clipProperty, GUIContent.none);

            EditorGUIUtility.labelWidth = MusicLabelWidth;
            EditorGUI.PropertyField(musicRect, isMusicProperty, new GUIContent("Music"));

            EditorGUIUtility.labelWidth = VolumeLabelWidth;
            EditorGUI.PropertyField(volumeRect, volumeProperty, new GUIContent("Volume"));

            EditorGUIUtility.labelWidth = previousLabelWidth;

            EditorGUI.EndProperty();
        }
    }
}
#endif
