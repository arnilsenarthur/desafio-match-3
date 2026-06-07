using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gazeus.DesafioMatch3.App
{
    public sealed class SceneTransitionService : MonoBehaviour
    {
        private const float FadeOutDuration = 0.35f;
        private const float FadeInDuration = 0.4f;

        private static SceneTransitionService _instance;

        [SerializeField]
        private CanvasGroup _overlayGroup;

        [SerializeField]
        private CanvasGroup _loadingGroup;

        private bool _isTransitioning;

        public static bool IsCovering =>
            _instance != null &&
            _instance._overlayGroup != null &&
            _instance._overlayGroup.alpha > 0.01f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            _instance = null;
        }

        public static void LoadScene(string sceneName, Action beforeLoad = null)
        {
            SceneTransitionService instance = EnsureInstance();
            if (instance == null)
            {
                beforeLoad?.Invoke();
                SceneManager.LoadScene(sceneName);
                return;
            }

            instance.StartCoroutine(instance.TransitionRoutine(sceneName, beforeLoad));
        }

        public static IEnumerator Cover()
        {
            SceneTransitionService instance = EnsureInstance();
            if (instance == null)
            {
                yield break;
            }

            SetLoadingVisible(true);
            yield return instance.FadeTo(1f, SettingsService.ScaleDuration(FadeOutDuration));
        }

        public static IEnumerator Reveal()
        {
            if (_instance == null || _instance._overlayGroup == null || _instance._overlayGroup.alpha <= 0.01f)
            {
                SetLoadingVisible(false);
                yield break;
            }

            yield return _instance.FadeTo(0f, SettingsService.ScaleDuration(FadeInDuration));
            SetLoadingVisible(false);
        }

        public static void SetLoadingVisible(bool visible)
        {
            if (_instance == null || _instance._loadingGroup == null)
            {
                return;
            }

            _instance._loadingGroup.alpha = visible ? 1f : 0f;
            _instance._loadingGroup.blocksRaycasts = false;
            _instance._loadingGroup.interactable = false;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            ResetVisualState();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private static SceneTransitionService EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindFirstObjectByType<SceneTransitionService>(FindObjectsInactive.Include);
            if (_instance == null)
            {
                Debug.LogError(
                    "SceneTransitionService not found. Add the SceneTransitionOverlay prefab to the scene.");
            }

            return _instance;
        }

        private void ResetVisualState()
        {
            _isTransitioning = false;

            if (_overlayGroup != null)
            {
                _overlayGroup.alpha = 0f;
                UpdateOverlayBlocking(0f);
            }

            SetLoadingVisible(false);
        }

        private IEnumerator TransitionRoutine(string sceneName, Action beforeLoad)
        {
            if (_isTransitioning || _overlayGroup == null)
            {
                yield break;
            }

            _isTransitioning = true;
            SetLoadingVisible(true);
            _overlayGroup.blocksRaycasts = true;

            yield return FadeTo(1f, SettingsService.ScaleDuration(FadeOutDuration));

            beforeLoad?.Invoke();

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
            if (loadOperation == null)
            {
                _isTransitioning = false;
                yield break;
            }

            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < 0.9f)
            {
                yield return null;
            }

            loadOperation.allowSceneActivation = true;

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;
            _isTransitioning = false;
        }

        private IEnumerator FadeTo(float endValue, float duration)
        {
            if (_overlayGroup == null)
            {
                yield break;
            }

            float startValue = _overlayGroup.alpha;
            if (Mathf.Approximately(startValue, endValue))
            {
                UpdateOverlayBlocking(endValue);
                yield break;
            }

            duration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _overlayGroup.alpha = Mathf.Lerp(startValue, endValue, t);
                yield return null;
            }

            _overlayGroup.alpha = endValue;
            UpdateOverlayBlocking(endValue);
        }

        private void UpdateOverlayBlocking(float alpha)
        {
            if (_overlayGroup == null)
            {
                return;
            }

            bool blocking = alpha > 0.01f;
            _overlayGroup.blocksRaycasts = blocking || _isTransitioning;
        }
    }
}
