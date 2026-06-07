using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Localization;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class GameOverView : MonoBehaviour
    {
        private const float NewHighScoreAnimDuration = 0.7f;
        private const float NewHighScorePeakScale = 1.22f;

        [SerializeField]
        private TMP_Text _reasonText;

        [SerializeField]
        private TMP_Text _finalScoreValueText;

        [SerializeField]
        private TMP_Text _bestScoreValueText;

        [SerializeField]
        private GameObject _newHighScoreRoot;

        [SerializeField]
        private TMP_Text _newHighScoreText;

        private string _reasonKey;
        private int _finalScore;
        private int _bestScore;
        private bool _isNewHighScore;
        private bool _visible;
        private bool _pendingPresentation;
        private Tween _newHighScoreTween;

        private void Awake() => ResolveReferences();

        private void OnEnable()
        {
            if (!_visible)
            {
                return;
            }

            RefreshPresentation(animate: _pendingPresentation);
            _pendingPresentation = false;
        }

        private void OnDisable()
        {
            StopNewHighScoreAnimation(resetVisuals: true);
        }

        public void Show(string reasonKey, int finalScore, int bestScore, bool isNewHighScore)
        {
            ResolveReferences();

            _reasonKey = reasonKey;
            _finalScore = finalScore;
            _bestScore = bestScore;
            _isNewHighScore = isNewHighScore;
            _visible = true;

            if (isActiveAndEnabled)
            {
                RefreshPresentation(animate: true);
                _pendingPresentation = false;
            }
            else
            {
                _pendingPresentation = true;
            }
        }

        public void Hide()
        {
            _visible = false;
            _pendingPresentation = false;
            StopNewHighScoreAnimation(resetVisuals: true);

            if (_newHighScoreRoot != null)
            {
                _newHighScoreRoot.SetActive(false);
            }
        }

        public void RefreshLanguage()
        {
            if (!_visible)
            {
                return;
            }

            RefreshPresentation(animate: false);
        }

        private void ResolveReferences()
        {
            if (_reasonText == null)
            {
                _reasonText = transform.Find("ReasonText")?.GetComponent<TMP_Text>();
            }

            if (_finalScoreValueText == null)
            {
                _finalScoreValueText = transform.Find("FinalScoreValue")?.GetComponent<TMP_Text>();
            }

            if (_bestScoreValueText == null)
            {
                _bestScoreValueText = transform.Find("BestScoreValue")?.GetComponent<TMP_Text>();
            }

            if (_newHighScoreRoot == null)
            {
                Transform newHighScore = transform.Find("NewHighScoreText");
                _newHighScoreRoot = newHighScore != null ? newHighScore.gameObject : null;
            }

            if (_newHighScoreText == null && _newHighScoreRoot != null)
            {
                _newHighScoreText = _newHighScoreRoot.GetComponent<TMP_Text>();
            }
        }

        private void RefreshPresentation(bool animate)
        {
            ApplyTexts();
            ApplyNewHighScoreState(animate);
        }

        private void ApplyTexts()
        {
            if (_reasonText != null)
            {
                _reasonText.text = LocalizationService.Localize(_reasonKey);
            }

            if (_finalScoreValueText != null)
            {
                _finalScoreValueText.text = _finalScore.ToString();
            }

            if (_bestScoreValueText != null)
            {
                _bestScoreValueText.text = _bestScore.ToString();
            }
        }

        private void ApplyNewHighScoreState(bool animate)
        {
            StopNewHighScoreAnimation(resetVisuals: true);

            if (_newHighScoreRoot == null)
            {
                return;
            }

            if (!_isNewHighScore)
            {
                _newHighScoreRoot.SetActive(false);
                return;
            }

            _newHighScoreRoot.SetActive(true);

            if (_newHighScoreText != null)
            {
                _newHighScoreText.text = LocalizationService.Localize(LocKeys.GameOverNewHighScore);
            }

            if (animate)
            {
                PlayNewHighScoreAnimation();
            }
            else if (_newHighScoreText != null)
            {
                _newHighScoreText.alpha = 1f;
                _newHighScoreText.transform.localScale = Vector3.one;
            }
        }

        private void PlayNewHighScoreAnimation()
        {
            if (_newHighScoreText == null)
            {
                return;
            }

            Transform target = _newHighScoreText.transform;
            target.DOKill();

            _newHighScoreText.alpha = 0f;
            target.localScale = Vector3.one * 0.65f;

            float duration = SettingsService.ScaleDuration(NewHighScoreAnimDuration);
            float fadeIn = duration * 0.35f;
            float pulseUp = duration * 0.4f;
            float settle = duration * 0.25f;

            _newHighScoreTween = DOTween.Sequence()
                .Append(DOTween.To(() => _newHighScoreText.alpha, value => _newHighScoreText.alpha = value, 1f, fadeIn)
                    .SetEase(Ease.OutQuad))
                .Join(target.DOScale(NewHighScorePeakScale, pulseUp).SetEase(Ease.OutBack))
                .Append(target.DOScale(1f, settle).SetEase(Ease.InOutSine))
                .SetTarget(target);
        }

        private void StopNewHighScoreAnimation(bool resetVisuals)
        {
            _newHighScoreTween?.Kill();
            _newHighScoreTween = null;

            if (_newHighScoreText == null)
            {
                return;
            }

            _newHighScoreText.transform.DOKill();

            if (resetVisuals)
            {
                _newHighScoreText.alpha = 1f;
                _newHighScoreText.transform.localScale = Vector3.one;
            }
        }
    }
}
