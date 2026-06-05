using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.Localization;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class GameHudView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _scoreText;

        [SerializeField]
        private TMP_Text _timeText;

        [SerializeField]
        private TMP_Text _statusText;

        [SerializeField]
        private TMP_Text _countdownText;

        private bool _bound;
        private string _difficultyId;
        private int _displayedScore;
        private int _displayedTimeSeconds = -1;
        private string _statusKey;
        private object[] _statusArgs = System.Array.Empty<object>();

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
            GameService.BoardRegenerated += OnBoardRegenerated;
            GameService.CountdownChanged += OnCountdownChanged;
            GameService.CascadeStep += OnCascadeStep;
            _bound = true;
        }

        private void OnEnable() => LocalizationService.LanguageChanged += OnLanguageChanged;

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
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
            GameService.BoardRegenerated -= OnBoardRegenerated;
            GameService.CountdownChanged -= OnCountdownChanged;
            GameService.CascadeStep -= OnCascadeStep;
            _bound = false;
        }

        private void OnGameStarted(GameStartedEventArgs args)
        {
            _difficultyId = args.DifficultyId;
            _displayedTimeSeconds = -1;
            ClearStatus();
            UpdateScoreText(0);
        }

        private void OnScoreChanged(ScoreChangedEventArgs args)
        {
            if (!this)
            {
                return;
            }

            UpdateScoreText(args.TotalScore);
        }

        private void UpdateScoreText(int score)
        {
            _displayedScore = score;

            if (_scoreText == null)
            {
                return;
            }

            int best = HighScoreStorage.GetDisplayBest(_difficultyId, score);
            _scoreText.text = LocalizationService.Localize(LocKeys.HudScoreLine, score, best);
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
            _timeText.text = LocalizationService.Localize(LocKeys.HudTimeLine, minutes, seconds);
        }

        private void OnCountdownChanged(CountdownChangedEventArgs args)
        {
            if (!this || _countdownText == null)
            {
                return;
            }

            if (!args.IsVisible)
            {
                _countdownText.text = string.Empty;
                return;
            }

            _countdownText.text = args.IsGo
                ? LocalizationService.Localize(LocKeys.CountdownGo)
                : args.StepNumber.ToString();
        }

        private void OnBoardRegenerated(BoardRegeneratedEventArgs args) =>
            SetStatus(LocKeys.StatusBoardReshuffled);

        private void OnCascadeStep(CascadeStepEventArgs args)
        {
            if (args.Sequence.SkullsCleared <= 0)
            {
                return;
            }

            string key = args.Sequence.SkullsCleared == 1
                ? LocKeys.StatusSkullPenalty
                : LocKeys.StatusSkullsPenalty;

            SetStatus(key, args.Sequence.SkullsCleared, args.Sequence.SkullTimePenalty);
        }

        private void OnGameEnded(GameEndedEventArgs args)
        {
            ClearStatus();
            UpdateScoreText(args.FinalScore);
        }

        private void SetStatus(string key, params object[] args)
        {
            _statusKey = key;
            _statusArgs = args ?? System.Array.Empty<object>();
            ApplyStatusText();
        }

        private void ClearStatus()
        {
            _statusKey = null;
            _statusArgs = System.Array.Empty<object>();
            if (_statusText != null)
            {
                _statusText.text = string.Empty;
            }
        }

        private void ApplyStatusText()
        {
            if (_statusText == null)
            {
                return;
            }

            _statusText.text = string.IsNullOrEmpty(_statusKey)
                ? string.Empty
                : LocalizationService.Localize(_statusKey, _statusArgs);
        }

        private void OnLanguageChanged()
        {
            if (!this)
            {
                return;
            }

            UpdateScoreText(_displayedScore);

            if (_displayedTimeSeconds >= 0)
            {
                ApplyTimeText(_displayedTimeSeconds);
            }

            ApplyStatusText();
        }
    }
}
