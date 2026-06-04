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
        [SerializeField] private UiPanelView _confirmPanel;
        [SerializeField] private TMP_Text _confirmTitleText;
        [SerializeField] private TMP_Text _confirmMessageText;
        [SerializeField] private TMP_Text _confirmButtonText;
        [SerializeField] private UiPanelView _gameOverPanel;
        [SerializeField] private TMP_Text _gameOverMessageText;

        private GameEvents _events;
        private bool _isPaused;
        private ConfirmAction _pendingConfirmAction;

        private enum ConfirmAction
        {
            None,
            Restart,
            MainMenu
        }

        private void Awake()
        {
            _pausePanel?.Hide();
            _confirmPanel?.Hide();
            _gameOverPanel?.Hide();
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

            if (_confirmPanel != null && _confirmPanel.gameObject.activeSelf)
            {
                CancelConfirm();
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
            _gameController.RestartCurrentGame();
        }

        public void ShowRestartConfirm()
        {
            ShowConfirm(
                ConfirmAction.Restart,
                "Restart Game?",
                "Current progress will be lost.",
                "Restart");
        }

        public void ShowMainMenuConfirm()
        {
            ShowConfirm(
                ConfirmAction.MainMenu,
                "Leave Game?",
                "Return to main menu? Current progress will be lost.",
                "Main Menu");
        }

        public void CancelConfirm()
        {
            _pendingConfirmAction = ConfirmAction.None;
            _confirmPanel?.Hide();
        }

        public void Confirm()
        {
            ConfirmAction action = _pendingConfirmAction;
            _pendingConfirmAction = ConfirmAction.None;
            _confirmPanel?.Hide();

            switch (action)
            {
                case ConfirmAction.Restart:
                    SetPaused(false);
                    _gameController.RestartCurrentGame();
                    break;
                case ConfirmAction.MainMenu:
                    SetPaused(false);
                    GoToMainMenu();
                    break;
            }
        }

        private void ShowConfirm(ConfirmAction action, string title, string message, string confirmLabel)
        {
            _pendingConfirmAction = action;

            if (_confirmTitleText != null)
            {
                _confirmTitleText.text = title;
            }

            if (_confirmMessageText != null)
            {
                _confirmMessageText.text = message;
            }

            if (_confirmButtonText != null)
            {
                _confirmButtonText.text = confirmLabel;
            }

            _confirmPanel?.Show();
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
                _pausePanel?.Show();
            }
            else
            {
                _pausePanel?.Hide();
                CancelConfirm();
            }

            _gameController.SetPaused(paused);
        }

        private void OnGameStarted(GameStartedEventArgs args)
        {
            _gameOverPanel?.Hide();
            CancelConfirm();

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

            _gameOverPanel?.Show();
            _gameController.SetPaused(true);
        }
    }
}
