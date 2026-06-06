using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.Localization;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class GameHudView : MonoBehaviour
    {
        private static readonly Color ScoreGreen = new(0.35f, 0.92f, 0.45f, 1f);
        private static readonly Color TimeRed = new(0.95f, 0.3f, 0.3f, 1f);

        private const float CountdownPulsePeakScale = 1.25f;
        private const float CountdownPulseDuration = 0.4f;
        private const float CountdownHideDuration = 0.3f;
        private const float ScoreHeartBeatScale = 1.14f;
        private const float ScoreHeartBeatStep = 0.1f;
        private const float DeltaPopupDuration = 0.75f;
        private const float DeltaPopupRise = 28f;

        [SerializeField]
        private TMP_Text _scoreText;

        [SerializeField]
        private TMP_Text _bestScoreValueText;

        [SerializeField]
        private TMP_Text _timeText;

        [SerializeField]
        private TMP_Text _scoreDeltaText;

        [SerializeField]
        private TMP_Text _timeDeltaText;

        [SerializeField]
        private TMP_Text _countdownText;

        private bool _bound;
        private string _difficultyId;
        private int _displayedScore;
        private int _displayedTimeSeconds = -1;
        private Tween _countdownTween;
        private Tween _scorePulseTween;
        private Tween _scoreDeltaTween;
        private Tween _timeDeltaTween;
        private Vector2 _scoreDeltaRestPosition;
        private Vector2 _timeDeltaRestPosition;
        private bool _countdownVisible;
        private bool _countdownIsGo;
        private int _countdownStepNumber;
        private bool _deltaPositionsCached;

        public void Bind()
        {
            Unbind();

            if (!GameService.IsActive)
            {
                return;
            }

            GameService.ScoreChanged += OnScoreChanged;
            GameService.TimeChanged += OnTimeChanged;
            GameService.GameStarted += OnGameStarted;
            GameService.GameEnded += OnGameEnded;
            GameService.CountdownChanged += OnCountdownChanged;
            GameService.CascadeStep += OnCascadeStep;
            _bound = true;
        }

        private void Awake() => CacheDeltaPopupPositions();

        private void OnEnable() => LocalizationService.LanguageChanged += OnLanguageChanged;

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            StopCountdownAnimation(resetVisuals: true);
            StopScoreAnimations();
            _scoreDeltaTween = StopDeltaPopup(_scoreDeltaText, _scoreDeltaTween);
            _timeDeltaTween = StopDeltaPopup(_timeDeltaText, _timeDeltaTween);
            Unbind();
        }

        private void OnDestroy() => Unbind();

        public void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            GameService.ScoreChanged -= OnScoreChanged;
            GameService.TimeChanged -= OnTimeChanged;
            GameService.GameStarted -= OnGameStarted;
            GameService.GameEnded -= OnGameEnded;
            GameService.CountdownChanged -= OnCountdownChanged;
            GameService.CascadeStep -= OnCascadeStep;
            _bound = false;
        }

        private void OnGameStarted(GameStartedEventArgs args)
        {
            _difficultyId = args.DifficultyId;
            _displayedTimeSeconds = -1;
            UpdateScoreText(0, pulse: false, showDelta: false);
        }

        private void OnScoreChanged(ScoreChangedEventArgs args)
        {
            if (!this)
            {
                return;
            }

            bool increased = args.Delta > 0;
            UpdateScoreText(args.TotalScore, pulse: increased, showDelta: increased, delta: args.Delta);
        }

        private void UpdateScoreText(int score, bool pulse, bool showDelta, int delta = 0)
        {
            _displayedScore = score;

            if (_scoreText != null)
            {
                _scoreText.text = score.ToString();
            }

            if (_bestScoreValueText != null)
            {
                int best = HighScoreStorage.GetDisplayBest(_difficultyId, score);
                _bestScoreValueText.text = best.ToString();
            }

            if (pulse)
            {
                PlayScoreHeartPulse();
            }

            if (showDelta && delta > 0)
            {
                PlayScoreDeltaPopup(delta);
            }
        }

        private void OnTimeChanged(TimeChangedEventArgs args)
        {
            if (!this)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, args.TimeRemaining));
            if (totalSeconds == _displayedTimeSeconds)
            {
                return;
            }

            _displayedTimeSeconds = totalSeconds;
            ApplyTimeText(totalSeconds);
        }

        private void ApplyTimeText(int totalSeconds)
        {
            if (_timeText == null)
            {
                return;
            }

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timeText.text = $"{minutes:00}:{seconds:00}";
        }

        private void OnCascadeStep(CascadeStepEventArgs args)
        {
            if (!this || args.Sequence.SkullsCleared <= 0 || args.Sequence.SkullTimePenalty <= 0f)
            {
                return;
            }

            PlayTimeDeltaPopup(args.Sequence.SkullTimePenalty);
        }

        private void OnCountdownChanged(CountdownChangedEventArgs args)
        {
            if (!this || _countdownText == null)
            {
                return;
            }

            if (!args.IsVisible)
            {
                _countdownVisible = false;
                PlayCountdownHide();
                return;
            }

            _countdownVisible = true;
            _countdownIsGo = args.IsGo;
            _countdownStepNumber = args.StepNumber;
            ApplyCountdownText();

            if (_countdownIsGo)
            {
                PlayCountdownHide();
                return;
            }

            PlayCountdownPulse();
        }

        private void ApplyCountdownText()
        {
            if (_countdownText == null)
            {
                return;
            }

            _countdownText.text = _countdownIsGo
                ? LocalizationService.Localize(LocKeys.CountdownGo)
                : _countdownStepNumber.ToString();
        }

        private void PlayCountdownPulse()
        {
            Transform target = _countdownText.transform;
            StopCountdownAnimation(resetVisuals: false);

            _countdownText.alpha = 1f;
            target.localScale = Vector3.one;

            float halfDuration = CountdownPulseDuration * 0.5f;

            _countdownTween = DOTween.Sequence()
                .Append(target.DOScale(CountdownPulsePeakScale, halfDuration).SetEase(Ease.OutQuad))
                .Append(target.DOScale(1f, halfDuration).SetEase(Ease.InOutSine))
                .SetTarget(target);
        }

        private void PlayCountdownHide()
        {
            if (string.IsNullOrEmpty(_countdownText.text))
            {
                StopCountdownAnimation(resetVisuals: true);
                return;
            }

            Transform target = _countdownText.transform;
            StopCountdownAnimation(resetVisuals: false);

            _countdownTween = DOTween.Sequence()
                .Join(target.DOScale(0f, CountdownHideDuration).SetEase(Ease.InBack))
                .Join(DOTween.To(() => _countdownText.alpha, value => _countdownText.alpha = value, 0f, CountdownHideDuration)
                    .SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    if (!this || _countdownText == null)
                    {
                        return;
                    }

                    _countdownText.text = string.Empty;
                    ResetCountdownVisuals();
                    _countdownTween = null;
                })
                .SetTarget(target);
        }

        private void StopCountdownAnimation(bool resetVisuals)
        {
            _countdownTween?.Kill();
            _countdownTween = null;

            if (_countdownText != null)
            {
                _countdownText.transform.DOKill();
            }

            if (resetVisuals)
            {
                ResetCountdownVisuals();
            }
        }

        private void ResetCountdownVisuals()
        {
            if (_countdownText == null)
            {
                return;
            }

            _countdownText.transform.localScale = Vector3.one;
            _countdownText.alpha = 1f;
        }

        private void PlayScoreHeartPulse()
        {
            if (_scoreText == null)
            {
                return;
            }

            Transform target = _scoreText.transform;
            _scorePulseTween?.Kill();
            target.DOKill();
            target.localScale = Vector3.one;

            float step = SettingsService.ScaleDuration(ScoreHeartBeatStep);

            _scorePulseTween = DOTween.Sequence()
                .Append(target.DOScale(ScoreHeartBeatScale, step).SetEase(Ease.OutQuad))
                .Append(target.DOScale(1f, step).SetEase(Ease.InQuad))
                .Append(target.DOScale(ScoreHeartBeatScale * 0.96f, step).SetEase(Ease.OutQuad))
                .Append(target.DOScale(1f, step).SetEase(Ease.InQuad))
                .SetTarget(target);
        }

        private void PlayScoreDeltaPopup(int delta)
        {
            _scoreDeltaTween = PlayDeltaPopup(
                _scoreDeltaText,
                _scoreDeltaTween,
                _scoreDeltaRestPosition,
                $"+{delta}",
                ScoreGreen);
        }

        private void PlayTimeDeltaPopup(float penaltySeconds)
        {
            _timeDeltaTween = PlayDeltaPopup(
                _timeDeltaText,
                _timeDeltaTween,
                _timeDeltaRestPosition,
                $"-{penaltySeconds:0.#}s",
                TimeRed);
        }

        private Tween PlayDeltaPopup(
            TMP_Text text,
            Tween activeTween,
            Vector2 restPosition,
            string message,
            Color color)
        {
            if (text == null)
            {
                return null;
            }

            CacheDeltaPopupPositions();

            StopDeltaPopup(text, activeTween);

            RectTransform rect = text.rectTransform;
            Transform target = text.transform;
            target.DOKill();

            text.gameObject.SetActive(true);
            text.text = message;
            text.color = new Color(color.r, color.g, color.b, 1f);
            text.alpha = 0f;
            rect.anchoredPosition = restPosition;
            rect.localScale = Vector3.one * 0.85f;

            float duration = SettingsService.ScaleDuration(DeltaPopupDuration);
            float fadeIn = duration * 0.22f;
            float hold = duration * 0.28f;
            float fadeOut = duration * 0.5f;
            float pulseUp = SettingsService.ScaleDuration(0.18f);
            Vector2 endPosition = restPosition + new Vector2(0f, DeltaPopupRise);

            return DOTween.Sequence()
                .Append(DOTween.To(() => text.alpha, value => text.alpha = value, 1f, fadeIn).SetEase(Ease.OutQuad))
                .Join(rect.DOScale(1.12f, pulseUp).SetEase(Ease.OutBack))
                .Append(rect.DOScale(1f, pulseUp * 0.6f).SetEase(Ease.InOutSine))
                .AppendInterval(hold)
                .Append(DOTween.To(() => text.alpha, value => text.alpha = value, 0f, fadeOut).SetEase(Ease.InQuad))
                .Join(DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, endPosition, fadeOut)
                    .SetEase(Ease.OutQuad))
                .Join(rect.DOScale(0.92f, fadeOut).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    if (text == null)
                    {
                        return;
                    }

                    text.gameObject.SetActive(false);
                    rect.anchoredPosition = restPosition;
                    rect.localScale = Vector3.one;
                    text.alpha = 0f;
                })
                .SetTarget(text);
        }

        private void StopScoreAnimations()
        {
            _scorePulseTween?.Kill();
            _scorePulseTween = null;

            if (_scoreText != null)
            {
                _scoreText.transform.DOKill();
                _scoreText.transform.localScale = Vector3.one;
            }
        }

        private static Tween StopDeltaPopup(TMP_Text text, Tween tween)
        {
            tween?.Kill();

            if (text == null)
            {
                return null;
            }

            text.transform.DOKill();
            text.gameObject.SetActive(false);
            text.alpha = 0f;
            return null;
        }

        private void CacheDeltaPopupPositions()
        {
            if (_deltaPositionsCached)
            {
                return;
            }

            if (_scoreDeltaText != null)
            {
                _scoreDeltaRestPosition = _scoreDeltaText.rectTransform.anchoredPosition;
            }

            if (_timeDeltaText != null)
            {
                _timeDeltaRestPosition = _timeDeltaText.rectTransform.anchoredPosition;
            }

            _deltaPositionsCached = true;
        }

        private void OnGameEnded(GameEndedEventArgs args) =>
            UpdateScoreText(args.FinalScore, pulse: false, showDelta: false);

        private void OnLanguageChanged()
        {
            if (!this)
            {
                return;
            }

            UpdateScoreText(_displayedScore, pulse: false, showDelta: false);

            if (_displayedTimeSeconds >= 0)
            {
                ApplyTimeText(_displayedTimeSeconds);
            }

            if (_countdownVisible)
            {
                ApplyCountdownText();
            }
        }
    }
}
