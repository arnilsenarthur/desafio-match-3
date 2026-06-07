#if UNITY_EDITOR
using Gazeus.DesafioMatch3.UI.Views;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    [CustomEditor(typeof(ResponsiveRectTransform))]
    public class ResponsiveRectTransformEditor : UnityEditor.Editor
    {
        private SerializedProperty _landscapeTarget;
        private SerializedProperty _portraitTarget;
        private SerializedProperty _editorPreviewLandscape;

        private void OnEnable()
        {
            _landscapeTarget = serializedObject.FindProperty("_landscapeTarget");
            _portraitTarget = serializedObject.FindProperty("_portraitTarget");
            _editorPreviewLandscape = serializedObject.FindProperty("_editorPreviewLandscape");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPreviewToolbar();

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(_landscapeTarget, new GUIContent("Landscape Target"));
            EditorGUILayout.PropertyField(_portraitTarget, new GUIContent("Portrait Target"));

            EditorGUILayout.HelpBox(
                "Follows Game view / Device Simulator size and orientation while not playing. " +
                "Landscape / Portrait buttons force a manual preview until the view changes again.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                ApplyToTargets();
            }
        }

        private void OnSceneGUI()
        {
            if (target is not ResponsiveRectTransform responsive)
            {
                return;
            }

            bool previewLandscape = responsive.UsesLandscapeLayout;
            DrawTargetBounds(responsive.LandscapeTarget, new Color(0.2f, 0.85f, 0.35f), previewLandscape);
            DrawTargetBounds(responsive.PortraitTarget, new Color(0.35f, 0.65f, 1f), !previewLandscape);
        }

        private void DrawPreviewToolbar()
        {
            bool isLandscape = _editorPreviewLandscape.boolValue;

            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (DrawPreviewButton("Landscape", isLandscape))
            {
                SetPreviewOrientation(landscape: true);
            }

            if (DrawPreviewButton("Portrait", !isLandscape))
            {
                SetPreviewOrientation(landscape: false);
            }

            EditorGUILayout.EndHorizontal();

            if (target is ResponsiveRectTransform responsive)
            {
                Vector2Int screenSize = new(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
                bool autoLandscape = screenSize.x >= screenSize.y;
                EditorGUILayout.LabelField(
                    $"Game View: {screenSize.x} x {screenSize.y} ({(autoLandscape ? "Landscape" : "Portrait")})",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.HelpBox(
                isLandscape
                    ? "Manual preview: landscape target."
                    : "Manual preview: portrait target.",
                MessageType.None);
        }

        private static bool DrawPreviewButton(string label, bool selected)
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = selected
                ? new Color(0.55f, 0.75f, 1f)
                : previousColor;

            bool pressed = GUILayout.Button(label, GUILayout.Height(24f));
            GUI.backgroundColor = previousColor;
            return pressed;
        }

        private void SetPreviewOrientation(bool landscape)
        {
            foreach (Object targetObject in targets)
            {
                if (targetObject is not ResponsiveRectTransform responsive)
                {
                    continue;
                }

                responsive.EditorSetPreviewLandscape(landscape);
                EditorUtility.SetDirty(responsive);
            }

            _editorPreviewLandscape.boolValue = landscape;
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }

        private void ApplyToTargets()
        {
            foreach (Object targetObject in targets)
            {
                if (targetObject is not ResponsiveRectTransform responsive)
                {
                    continue;
                }

                responsive.ForceRefresh();
                EditorUtility.SetDirty(responsive);
            }
        }

        private static void DrawTargetBounds(RectTransform target, Color color, bool active)
        {
            if (target == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Color fill = new(color.r, color.g, color.b, active ? 0.12f : 0.05f);
            Color outline = active ? color : new Color(color.r, color.g, color.b, 0.45f);

            Handles.DrawSolidRectangleWithOutline(corners, fill, outline);

            Vector3 labelPosition = (corners[1] + corners[2]) * 0.5f + Vector3.up * 4f;
            Handles.Label(labelPosition, target.name, EditorStyles.whiteMiniLabel);
        }
    }
}
#endif
