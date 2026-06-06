using DG.Tweening;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class UiPanelView : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup _modal;

        [SerializeField]
        private float _animationDuration = 0.25f;

        [SerializeField]
        private float _showScaleFrom = 0.9f;

        [SerializeField]
        private Ease _showEase = Ease.OutBack;

        [SerializeField]
        private Ease _hideEase = Ease.InBack;

        private CanvasGroup _rootGroup;
        private RectTransform _modalRect;
        private Tween _activeTween;
        private bool _isAnimating;
        private bool _initialized;

        public bool IsVisible => gameObject.activeSelf;
        public bool IsAnimating => _isAnimating;

        public static void HideAllOnLoad(params UiPanelView[] panels)
        {
            if (panels == null)
            {
                return;
            }

            for (int i = 0; i < panels.Length; i++)
            {
                panels[i]?.Hide(animated: false);
            }
        }

        public void Show(bool animated = true)
        {
            EnsureInitialized();
            KillActiveTween();
            gameObject.SetActive(true);
            OnBeforeShow();

            if (_modal == null || !animated)
            {
                ResetModalInstant();
                SetModalInteraction(true);
                return;
            }

            SetRootInteractionBlocked(true);
            SetModalInteraction(true);
            PrepareModalForShow();

            _isAnimating = true;
            _activeTween = CreateShowTween()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(OnShowComplete);
        }

        public void Hide(bool animated = true)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            EnsureInitialized();

            if (_modal == null || !animated)
            {
                CompleteHide();
                return;
            }

            KillActiveTween();
            SetRootInteractionBlocked(true);
            SetModalInteraction(false);
            _isAnimating = true;

            _activeTween = CreateHideTween()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(OnHideComplete);
        }

        protected virtual void OnBeforeShow()
        {
        }

        protected virtual void OnAfterHide()
        {
        }

        private void OnDisable()
        {
            KillActiveTween();
            _isAnimating = false;
            SetRootInteractionBlocked(false);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            TryGetComponent(out _rootGroup);

            if (_modal != null)
            {
                _modalRect = _modal.transform as RectTransform;
            }
        }

        private void OnShowComplete()
        {
            _isAnimating = false;
            _activeTween = null;
            ResetModalInstant();
            SetRootInteractionBlocked(false);
            SetModalInteraction(true);
        }

        private void OnHideComplete()
        {
            CompleteHide();
        }

        private void CompleteHide()
        {
            KillActiveTween();
            _isAnimating = false;
            SetRootInteractionBlocked(false);
            ResetModalInstant();
            OnAfterHide();
            gameObject.SetActive(false);
        }

        private void PrepareModalForShow()
        {
            _modal.alpha = 0f;

            if (_modalRect != null)
            {
                _modalRect.localScale = Vector3.one * _showScaleFrom;
            }
        }

        private Sequence CreateShowTween()
        {
            float duration = Mathf.Max(0.01f, _animationDuration);
            Sequence sequence = DOTween.Sequence();
            sequence.Join(TweenModalAlpha(1f, duration, _showEase));

            if (_modalRect != null)
            {
                sequence.Join(_modalRect.DOScale(1f, duration).SetEase(_showEase));
            }

            return sequence;
        }

        private Sequence CreateHideTween()
        {
            float duration = Mathf.Max(0.01f, _animationDuration);
            Sequence sequence = DOTween.Sequence();
            sequence.Join(TweenModalAlpha(0f, duration, _hideEase));

            if (_modalRect != null)
            {
                sequence.Join(_modalRect.DOScale(_showScaleFrom, duration).SetEase(_hideEase));
            }

            return sequence;
        }

        private Tween TweenModalAlpha(float endValue, float duration, Ease ease)
        {
            return DOTween.To(() => _modal.alpha, value => _modal.alpha = value, endValue, duration)
                .SetEase(ease)
                .SetTarget(_modal);
        }

        private void ResetModalInstant()
        {
            if (_modal == null)
            {
                return;
            }

            _modal.alpha = 1f;

            if (_modalRect != null)
            {
                _modalRect.localScale = Vector3.one;
            }
        }

        private void SetModalInteraction(bool enabled)
        {
            if (_modal == null)
            {
                return;
            }

            _modal.interactable = enabled;
            _modal.blocksRaycasts = enabled;
        }

        private void SetRootInteractionBlocked(bool blocked)
        {
            if (_rootGroup == null)
            {
                return;
            }

            _rootGroup.interactable = !blocked;
            _rootGroup.blocksRaycasts = true;
        }

        private void KillActiveTween()
        {
            if (_activeTween == null)
            {
                return;
            }

            _activeTween.Kill();
            _activeTween = null;
        }
    }
}
