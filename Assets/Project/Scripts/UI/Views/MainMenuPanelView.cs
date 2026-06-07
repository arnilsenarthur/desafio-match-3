using System.Collections;
using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Audio;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Localization;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class MainMenuPanelView : UIPanelView
    {
        private const float EnterDuration = 0.35f;
        private const float EnterStagger = 0.08f;

        [SerializeField]
        private GameConfig _gameConfig;

        [SerializeField]
        private TMP_Text _highScoreDifficultyLabel;

        [SerializeField]
        private TMP_Text _highScoreValueLabel;

        [SerializeField]
        private float _highScoreCycleSeconds = 3f;

        [SerializeField]
        private RectTransform[] _enterElements;

        private Coroutine _highScoreCycleCoroutine;
        private int _highScoreCycleIndex;
        private Tween _enterTween;

        private void OnEnable() => LocalizationService.LanguageChanged += OnLanguageChanged;

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            StopHighScoreCycle();
            _enterTween?.Kill();
            _enterTween = null;
        }

        protected override void OnBeforeShow()
        {
            PlayEnterAnimation();
            StartHighScoreCycle();
        }

        protected override void OnAfterHide()
        {
            StopHighScoreCycle();
            _enterTween?.Kill();
            _enterTween = null;
        }

        public IEnumerator WaitForEnterAnimation()
        {
            if (_enterTween != null && _enterTween.IsActive())
            {
                yield return _enterTween.WaitForCompletion();
            }
        }

        private void OnLanguageChanged()
        {
            if (!this || !IsVisible)
            {
                return;
            }

            if (CanCycleHighScores())
            {
                ShowHighScoreEntry(_highScoreCycleIndex);
            }
        }

        private void PlayEnterAnimation()
        {
            _enterTween?.Kill();

            RectTransform[] targets = ResolveEnterElements();
            if (targets == null || targets.Length == 0)
            {
                return;
            }

            float duration = SettingsService.ScaleDuration(EnterDuration);
            float stagger = SettingsService.ScaleDuration(EnterStagger);
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < targets.Length; i++)
            {
                RectTransform target = targets[i];
                if (target == null)
                {
                    continue;
                }

                target.localScale = Vector3.zero;
                float delay = i * stagger;
                sequence.InsertCallback(delay, PlayEnterTileSlideSound);
                sequence.Insert(
                    delay,
                    target.DOScale(1f, duration).SetEase(Ease.OutBack));
            }

            _enterTween = sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private static void PlayEnterTileSlideSound() =>
            AudioService.PlaySfx(AudioKeys.GameplayTileSlide);

        private RectTransform[] ResolveEnterElements()
        {
            if (_enterElements != null && _enterElements.Length > 0)
            {
                return _enterElements;
            }

            RectTransform root = transform as RectTransform;
            if (root == null || root.childCount == 0)
            {
                return System.Array.Empty<RectTransform>();
            }

            var targets = new RectTransform[root.childCount];
            for (int i = 0; i < root.childCount; i++)
            {
                targets[i] = root.GetChild(i) as RectTransform;
            }

            return targets;
        }

        private void StartHighScoreCycle()
        {
            StopHighScoreCycle();

            if (!CanCycleHighScores())
            {
                ClearHighScoreLabels();
                return;
            }

            _highScoreCycleIndex = 0;
            ShowHighScoreEntry(_highScoreCycleIndex);
            _highScoreCycleCoroutine = StartCoroutine(CycleHighScores());
        }

        private void StopHighScoreCycle()
        {
            if (_highScoreCycleCoroutine == null)
            {
                return;
            }

            StopCoroutine(_highScoreCycleCoroutine);
            _highScoreCycleCoroutine = null;
        }

        private IEnumerator CycleHighScores()
        {
            WaitForSeconds wait = new(_highScoreCycleSeconds);

            while (true)
            {
                yield return wait;

                if (!this || !isActiveAndEnabled || !IsVisible || !CanCycleHighScores())
                {
                    yield break;
                }

                _highScoreCycleIndex = (_highScoreCycleIndex + 1) % _gameConfig.Difficulties.Length;
                ShowHighScoreEntry(_highScoreCycleIndex);
            }
        }

        private bool CanCycleHighScores() =>
            _gameConfig != null &&
            _highScoreDifficultyLabel != null &&
            _highScoreValueLabel != null &&
            _gameConfig.Difficulties is { Length: > 0 };

        private void ShowHighScoreEntry(int index)
        {
            GameDifficultySettings difficulty = _gameConfig.Difficulties[index];
            _highScoreDifficultyLabel.text = LocalizationService.LocalizeDifficulty(difficulty.Id);
            _highScoreValueLabel.text = HighScoreStorage.Get(difficulty.Id).ToString();
        }

        private void ClearHighScoreLabels()
        {
            if (_highScoreDifficultyLabel != null)
            {
                _highScoreDifficultyLabel.text = string.Empty;
            }

            if (_highScoreValueLabel != null)
            {
                _highScoreValueLabel.text = string.Empty;
            }
        }
    }
}
