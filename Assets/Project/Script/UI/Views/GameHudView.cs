using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.UI;
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

        private GameEvents _events;
        private string _difficultyId;
        private int _storedBest;
        private int _displayedTimeSeconds = -1;

        public void Bind(GameEvents events)
        {
            Unbind();
            _events = events;
            _events.ScoreChanged += OnScoreChanged;
            _events.TimeChanged += OnTimeChanged;
            _events.GameStarted += OnGameStarted;
            _events.GameEnded += OnGameEnded;
            _events.BoardRegenerated += OnBoardRegenerated;
            _events.CountdownChanged += OnCountdownChanged;
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (_events == null)
            {
                return;
            }

            _events.ScoreChanged -= OnScoreChanged;
            _events.TimeChanged -= OnTimeChanged;
            _events.GameStarted -= OnGameStarted;
            _events.GameEnded -= OnGameEnded;
            _events.BoardRegenerated -= OnBoardRegenerated;
            _events.CountdownChanged -= OnCountdownChanged;
            _events = null;
        }

        private void OnGameStarted(GameStartedEventArgs args)
        {
            _difficultyId = args.DifficultyId;
            _storedBest = HighScoreStorage.Get(_difficultyId);
            _displayedTimeSeconds = -1;
            SetStatus(string.Empty);
            UpdateScoreText(0);
        }

        private void OnScoreChanged(ScoreChangedEventArgs args) => UpdateScoreText(args.TotalScore);

        private void UpdateScoreText(int score)
        {
            if (_scoreText == null)
            {
                return;
            }

            int best = HighScoreStorage.GetDisplayBest(_difficultyId, score);
            _scoreText.text = UiText.ScoreLine(score, best);
        }

        private void OnTimeChanged(TimeChangedEventArgs args)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, args.TimeRemaining));
            if (totalSeconds == _displayedTimeSeconds)
            {
                return;
            }

            _displayedTimeSeconds = totalSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timeText.text = UiText.TimeLine(minutes, seconds);
        }

        private void OnCountdownChanged(CountdownChangedEventArgs args)
        {
            if (_countdownText == null)
            {
                return;
            }

            _countdownText.text = args.IsVisible ? args.DisplayText : string.Empty;
        }

        private void OnBoardRegenerated(BoardRegeneratedEventArgs args) =>
            SetStatus(UiText.StatusBoardReshuffled);

        private void OnGameEnded(GameEndedEventArgs args)
        {
            SetStatus(string.Empty);
            _storedBest = HighScoreStorage.Get(_difficultyId);
            UpdateScoreText(args.FinalScore);
        }

        private void SetStatus(string message) => _statusText.text = message;
    }
}
