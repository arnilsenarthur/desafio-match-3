using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ResponsiveRectTransform : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _landscapeTarget;

        [SerializeField]
        private RectTransform _portraitTarget;

        [SerializeField, HideInInspector]
        private bool _editorPreviewLandscape = true;

        private RectTransform _rectTransform;
        private bool _isApplying;

        public RectTransform LandscapeTarget => _landscapeTarget;

        public RectTransform PortraitTarget => _portraitTarget;

        public bool EditorPreviewLandscape => _editorPreviewLandscape;

#if UNITY_EDITOR
        public void EditorSetPreviewLandscape(bool landscape)
        {
            if (_editorPreviewLandscape == landscape)
            {
                return;
            }

            _editorPreviewLandscape = landscape;
            ApplyCurrentTarget();
        }
#endif

        private void OnEnable()
        {
            _rectTransform = transform as RectTransform;
            ApplyCurrentTarget();
        }

        private void Update()
        {
            ApplyCurrentTarget();
        }

        public void ApplyCurrentTarget()
        {
            if (_isApplying)
            {
                return;
            }

            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            RectTransform source = ResolveIsLandscape() ? _landscapeTarget : _portraitTarget;
            if (_rectTransform == null || source == null)
            {
                return;
            }

            _isApplying = true;
            CopyRectTransformLayout(_rectTransform, source);
            _isApplying = false;
        }

        public static void CopyRectTransformLayout(RectTransform destination, RectTransform source)
        {
            if (destination == null || source == null || destination == source)
            {
                return;
            }

            destination.pivot = source.pivot;
            destination.rotation = source.rotation;
            ApplyWorldScale(destination, source.lossyScale);

            Vector2 size = source.rect.size;
            destination.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            destination.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            destination.position = source.position;
        }

        private static void ApplyWorldScale(RectTransform destination, Vector3 worldScale)
        {
            Transform parent = destination.parent;
            if (parent == null)
            {
                destination.localScale = worldScale;
                return;
            }

            Vector3 parentWorldScale = parent.lossyScale;
            destination.localScale = new Vector3(
                DivideWorldScale(worldScale.x, parentWorldScale.x),
                DivideWorldScale(worldScale.y, parentWorldScale.y),
                DivideWorldScale(worldScale.z, parentWorldScale.z));
        }

        private static float DivideWorldScale(float value, float parentValue)
        {
            if (Mathf.Approximately(parentValue, 0f))
            {
                return value;
            }

            return value / parentValue;
        }

        private bool ResolveIsLandscape()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return _editorPreviewLandscape;
            }
#endif

            return Screen.width >= Screen.height;
        }
    }
}
