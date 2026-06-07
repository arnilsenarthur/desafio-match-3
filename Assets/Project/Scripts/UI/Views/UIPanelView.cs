using DG.Tweening;
using Gazeus.DesafioMatch3.Audio;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class UIPanelView : MonoBehaviour
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

        [SerializeField]
        private bool _playPopupSound;

        private CanvasGroup _rootGroup;
        private RectTransform _modalRect;
        private Tween _activeTween;
        private bool _isAnimating;
        private bool _initialized;

        public bool IsVisible => IsAlive() && gameObject.activeSelf;

        public bool IsAnimating => _isAnimating;

        public static void HideAllOnLoad(params UIPanelView[] panels)
        {
            if (panels == null)
            {
                return;
            }

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                {
                    panels[i].Hide(animated: false);
                }
            }
        }

        public void Show(bool animated = true)
        {
            if (!IsAlive())
            {
                return;
            }

            EnsureInitialized();
            CancelActiveTransition();
            gameObject.SetActive(true);
            OnBeforeShow();
            PlayOpenSound();

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
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OnShowComplete);
        }

        public void Hide(bool animated = true)
        {
            if (!IsAlive() || !gameObject.activeSelf)
            {
                return;
            }

            EnsureInitialized();

            if (_modal == null || !animated)
            {
                CompleteHide();
                return;
            }

            CancelActiveTransition();
            SetRootInteractionBlocked(true);
            SetModalInteraction(false);
            _isAnimating = true;

            _activeTween = CreateHideTween()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OnHideComplete);
        }

        protected virtual string GetOpenSoundKey() =>
            _playPopupSound ? AudioKeys.UIPopup : null;

        protected void PlayOpenSound()
        {
            string key = GetOpenSoundKey();
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            AudioService.PlaySfx(key);
        }

        protected virtual void OnBeforeShow()
        {
        }

        protected virtual void OnAfterHide()
        {
        }

        private void OnDisable()
        {
            CancelActiveTransition();
        }

        private void OnDestroy()
        {
            CancelActiveTransition();
        }

        private bool IsAlive() => this != null;

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
            if (!IsAlive())
            {
                return;
            }

            _isAnimating = false;
            _activeTween = null;
            ResetModalInstant();
            SetRootInteractionBlocked(false);
            SetModalInteraction(true);
        }

        private void OnHideComplete()
        {
            if (!IsAlive())
            {
                return;
            }

            CompleteHide();
        }

        private void CompleteHide()
        {
            if (!IsAlive())
            {
                return;
            }

            CancelActiveTransition();
            ResetModalInstant();
            OnAfterHide();
            gameObject.SetActive(false);
        }

        private void CancelActiveTransition()
        {
            if (_activeTween != null)
            {
                _activeTween.Kill();
                _activeTween = null;
            }

            _isAnimating = false;
            SetRootInteractionBlocked(false);
        }

        private void PrepareModalForShow()
        {
            if (_modal == null)
            {
                return;
            }

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
    }
}
