using Gazeus.DesafioMatch3.Core;
using Gazeus.DesafioMatch3.Views;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class GameplayUiController : MonoBehaviour
    {
        [SerializeField] private GameController _gameController;
        [SerializeField] private UiPanelView _pausePanel;
        [SerializeField] private UiPanelView _gameOverPanel;
        [SerializeField] private TMP_Text _gameOverMessageText;

        private GameEvents _events;
        private bool _isPaused;

        private void Awake()
        {
            _pausePanel.Hide();
            _gameOverPanel.Hide();
        }

        private void Start()
        {
            _events = _gameController.GameService.Events;
            _events.GameStarted += OnGameStarted;
            _events.GameEnded += OnGameEnded;
        }

        private void OnDestroy()
        {
            if (_events != null)
            {
                _events.GameStarted -= OnGameStarted;
                _events.GameEnded -= OnGameEnded;
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (_gameOverPanel.gameObject.activeSelf)
            {
                return;
            }

            TogglePause();
        }

        public void Resume()
        {
            SetPaused(false);
        }

        public void GoToMainMenu()
        {
            SceneLoader.LoadMainMenu();
        }

        public void Retry()
        {
            SceneLoader.ReloadGameplay();
        }

        private void TogglePause()
        {
            if (_gameController.GameService.IsGameOver)
            {
                return;
            }

            SetPaused(!_isPaused);
        }

        private void SetPaused(bool paused)
        {
            _isPaused = paused;

            if (paused)
            {
                _pausePanel.Show();
            }
            else
            {
                _pausePanel.Hide();
            }

            _gameController.SetPaused(paused);
        }

        private void OnGameStarted(GameStartedEventArgs args)
        {
            _gameOverPanel.Hide();

            if (_isPaused)
            {
                SetPaused(false);
            }
        }

        private void OnGameEnded(GameEndedEventArgs args)
        {
            SetPaused(false);

            _gameOverMessageText.text = args.Reason switch
            {
                GameEndReason.TargetScoreReached => $"Goal reached!\nScore: {args.FinalScore}",
                GameEndReason.TimeUp => $"Time's up!\nScore: {args.FinalScore}",
                _ => $"Score: {args.FinalScore}"
            };

            _gameOverPanel.Show();
            _gameController.SetPaused(true);
        }
    }
}
