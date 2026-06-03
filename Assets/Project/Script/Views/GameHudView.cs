using Gazeus.DesafioMatch3.Core;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Views
{
    public class GameHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _statusText;

        private GameEvents _events;

        public void Bind(GameEvents events)
        {
            Unbind();
            _events = events;
            _events.ScoreChanged += OnScoreChanged;
            _events.TimeChanged += OnTimeChanged;
            _events.GameStarted += OnGameStarted;
            _events.GameEnded += OnGameEnded;
            _events.BoardRegenerated += OnBoardRegenerated;
        }

        private void OnDestroy()
        {
            Unbind();
        }

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
            _events = null;
        }

        private void OnGameStarted(GameStartedEventArgs args)
        {
            SetStatus(string.Empty);
        }

        private void OnScoreChanged(ScoreChangedEventArgs args)
        {
            _scoreText.text = $"Score: {args.TotalScore}";
        }

        private void OnTimeChanged(TimeChangedEventArgs args)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, args.TimeRemaining));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        private void OnBoardRegenerated(BoardRegeneratedEventArgs args)
        {
            SetStatus("Board reshuffled!");
        }

        private void OnGameEnded(GameEndedEventArgs args)
        {
            SetStatus(string.Empty);
        }

        private void SetStatus(string message)
        {
            _statusText.text = message;
        }
    }
}
