using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeviceScreen = UnityEngine.Device.Screen;

namespace Gazeus.DesafioMatch3.UI.Views
{
    [ExecuteAlways]
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

        [SerializeField, HideInInspector]
        private bool _editorManualPreview;

        private RectTransform _rectTransform;
        private bool _isApplying;
        private bool _runtimeRefreshPending;
        private Vector2Int _lastAppliedScreenSize = new(int.MinValue, int.MinValue);

        public RectTransform LandscapeTarget => _landscapeTarget;

        public RectTransform PortraitTarget => _portraitTarget;

        public bool EditorPreviewLandscape => _editorPreviewLandscape;

        public bool UsesLandscapeLayout => ResolveIsLandscape();

#if UNITY_EDITOR
        public void EditorSetPreviewLandscape(bool landscape)
        {
            _editorManualPreview = true;
            _editorPreviewLandscape = landscape;
            ForceRefresh();
        }

        public bool EditorRefreshIfNeeded()
        {
            if (Application.isPlaying)
            {
                return false;
            }

            Vector2Int screenSize = GetScreenSize();
            if (screenSize != _lastAppliedScreenSize)
            {
                _editorManualPreview = false;
            }
            else if (!HaveTargetsChanged())
            {
                return false;
            }

            ApplyCurrentTarget();
            return true;
        }

        private bool HaveTargetsChanged()
        {
            return (_landscapeTarget != null && _landscapeTarget.hasChanged) ||
                   (_portraitTarget != null && _portraitTarget.hasChanged);
        }

        private void ResetTargetChangeFlags()
        {
            if (_landscapeTarget != null)
            {
                _landscapeTarget.hasChanged = false;
            }

            if (_portraitTarget != null)
            {
                _portraitTarget.hasChanged = false;
            }
        }
#endif

        private void OnEnable()
        {
            _rectTransform = transform as RectTransform;

#if UNITY_EDITOR
            Canvas.willRenderCanvases += OnCanvasPreRender;

            if (!Application.isPlaying)
            {
                ResponsiveRectTransformEditorBridge.Register(this);
                ForceRefresh();
                return;
            }
#endif

            _runtimeRefreshPending = true;
        }

        private void Start()
        {
            if (!Application.isPlaying || !_runtimeRefreshPending)
            {
                return;
            }

            _runtimeRefreshPending = false;
            StartCoroutine(ApplyWhenLayoutReady());
        }

        private System.Collections.IEnumerator ApplyWhenLayoutReady()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ForceRefresh();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            Canvas.willRenderCanvases -= OnCanvasPreRender;
            ResponsiveRectTransformEditorBridge.Unregister(this);
#endif

            _runtimeRefreshPending = false;
        }

        private void OnCanvasPreRender()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorRefreshIfNeeded();
            }
#endif
        }

        private void Update()
        {
            if (!Application.isPlaying || _isApplying)
            {
                return;
            }

            Vector2Int screenSize = GetScreenSize();
            if (screenSize == _lastAppliedScreenSize)
            {
                return;
            }

            ApplyCurrentTarget();
        }

        public void ForceRefresh()
        {
            _lastAppliedScreenSize = new Vector2Int(int.MinValue, int.MinValue);
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
            try
            {
                PrepareSourceLayout(source);
                CopyRectTransformLayout(_rectTransform, source);
                _lastAppliedScreenSize = GetScreenSize();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(_rectTransform);
                    ResetTargetChangeFlags();
                }
#endif
            }
            finally
            {
                _isApplying = false;
            }
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

        private static void PrepareSourceLayout(RectTransform source)
        {
            var activatedObjects = new List<GameObject>();
            ActivateInactiveAncestors(source, activatedObjects);

            try
            {
                Canvas canvas = source.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
                }

                Canvas.ForceUpdateCanvases();
            }
            finally
            {
                RestoreTemporarilyActivatedObjects(activatedObjects);
            }
        }

        private static void ActivateInactiveAncestors(RectTransform source, List<GameObject> activatedObjects)
        {
            Transform current = source;
            while (current != null)
            {
                GameObject gameObject = current.gameObject;
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                    activatedObjects.Add(gameObject);
                }

                current = current.parent;
            }
        }

        private static void RestoreTemporarilyActivatedObjects(List<GameObject> activatedObjects)
        {
            for (int i = activatedObjects.Count - 1; i >= 0; i--)
            {
                GameObject gameObject = activatedObjects[i];
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private static Vector2Int GetScreenSize() =>
            new(DeviceScreen.width, DeviceScreen.height);

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
            if (!Application.isPlaying && _editorManualPreview)
            {
                return _editorPreviewLandscape;
            }
#endif

            Vector2Int screenSize = GetScreenSize();
            return screenSize.x >= screenSize.y;
        }
    }

#if UNITY_EDITOR
    internal static class ResponsiveRectTransformEditorBridge
    {
        private static readonly HashSet<ResponsiveRectTransform> Instances = new();
        private static bool _updateHooked;

        public static void Register(ResponsiveRectTransform instance)
        {
            if (instance == null)
            {
                return;
            }

            Instances.Add(instance);
            EnsureUpdateHooked();
        }

        public static void Unregister(ResponsiveRectTransform instance)
        {
            if (instance == null)
            {
                return;
            }

            Instances.Remove(instance);

            if (Instances.Count == 0 && _updateHooked)
            {
                UnityEditor.EditorApplication.update -= OnEditorUpdate;
                _updateHooked = false;
            }
        }

        private static void EnsureUpdateHooked()
        {
            if (_updateHooked)
            {
                return;
            }

            UnityEditor.EditorApplication.update += OnEditorUpdate;
            _updateHooked = true;
        }

        private static void OnEditorUpdate()
        {
            if (Application.isPlaying || Instances.Count == 0)
            {
                return;
            }

            PruneDestroyedInstances();

            bool repaint = false;
            foreach (ResponsiveRectTransform instance in Instances)
            {
                if (instance == null)
                {
                    continue;
                }

                if (instance.EditorRefreshIfNeeded())
                {
                    repaint = true;
                }
            }

            if (repaint)
            {
                UnityEditor.SceneView.RepaintAll();
            }
        }

        private static void PruneDestroyedInstances()
        {
            if (Instances.Count == 0)
            {
                return;
            }

            var alive = new List<ResponsiveRectTransform>();
            foreach (ResponsiveRectTransform instance in Instances)
            {
                if (instance != null)
                {
                    alive.Add(instance);
                }
            }

            if (alive.Count == Instances.Count)
            {
                return;
            }

            Instances.Clear();
            for (int i = 0; i < alive.Count; i++)
            {
                Instances.Add(alive[i]);
            }
        }
    }
#endif
}
