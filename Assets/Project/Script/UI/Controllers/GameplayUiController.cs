using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.UI;
using Gazeus.DesafioMatch3.UI.Views;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Controllers
{
    [DefaultExecutionOrder(0)]
    public class GameplayUiController : MonoBehaviour
    {
        [SerializeField]
        private GameController _gameController;

        [SerializeField]
        private UiPanelView _pausePanel;

        [SerializeField]
        private UiPanelView _confirmPanel;

        [SerializeField]
        private TMP_Text _confirmTitleText;

        [SerializeField]
        private TMP_Text _confirmMessageText;

        [SerializeField]
        private TMP_Text _confirmButtonText;

        [SerializeField]
        private UiPanelView _gameOverPanel;

        [SerializeField]
        private TMP_Text _gameOverMessageText;

        private GameEvents _events;
        private ConfirmAction _pendingConfirmAction;

        private enum ConfirmAction
        {
            None,
            Restart,
            MainMenu
        }

        private readonly struct ConfirmDialogContent
        {
            public ConfirmAction Action { get; }
            public string Title { get; }
            public string Message { get; }
            public string ConfirmLabel { get; }

            public ConfirmDialogContent(ConfirmAction action, string title, string message, string confirmLabel)
            {
                Action = action;
                Title = title;
                Message = message;
                ConfirmLabel = confirmLabel;
            }
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
            if (_events == null)
            {
                return;
            }

            _events.GameStarted -= OnGameStarted;
            _events.GameEnded -= OnGameEnded;
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

            if (_gameOverPanel != null && _gameOverPanel.gameObject.activeSelf)
            {
                return;
            }

            TogglePause();
        }

        public void Resume() => ApplyPause(false);

        public void GoToMainMenu() => SceneLoader.LoadMainMenu();

        public void Retry() => _gameController.RestartCurrentGame();

        public void ShowRestartConfirm() => ShowConfirm(new ConfirmDialogContent(
            ConfirmAction.Restart,
            UiText.ConfirmRestartTitle,
            UiText.ConfirmRestartMessage,
            UiText.ConfirmRestartButton));

        public void ShowMainMenuConfirm() => ShowConfirm(new ConfirmDialogContent(
            ConfirmAction.MainMenu,
            UiText.ConfirmMainMenuTitle,
            UiText.ConfirmMainMenuMessage,
            UiText.ConfirmMainMenuButton));

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
                    ApplyPause(false);
                    _gameController.RestartCurrentGame();
                    break;
                case ConfirmAction.MainMenu:
                    GoToMainMenu();
                    break;
            }
        }

        private void ShowConfirm(ConfirmDialogContent content)
        {
            _pendingConfirmAction = content.Action;

            if (_confirmTitleText != null)
            {
                _confirmTitleText.text = content.Title;
            }

            if (_confirmMessageText != null)
            {
                _confirmMessageText.text = content.Message;
            }

            if (_confirmButtonText != null)
            {
                _confirmButtonText.text = content.ConfirmLabel;
            }

            _confirmPanel?.Show();
        }

        private void TogglePause()
        {
            if (_gameController.GameService.IsGameOver)
            {
                return;
            }

            ApplyPause(!_gameController.IsPaused);
        }

        private void ApplyPause(bool paused)
        {
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
            ApplyPause(false);
        }

        private void OnGameEnded(GameEndedEventArgs args)
        {
            _pausePanel?.Hide();
            CancelConfirm();

            string difficultyId = GameRunContext.SelectedDifficultyId;
            bool isNewHighScore = HighScoreStorage.TrySetHighScore(difficultyId, args.FinalScore);
            int bestScore = HighScoreStorage.Get(difficultyId);

            string reasonLine = args.Reason switch
            {
                GameEndReason.TargetScoreReached => UiText.GameOverGoalReached,
                GameEndReason.TimeUp => UiText.GameOverTimeUp,
                _ => UiText.GameOverDefault
            };

            _gameOverMessageText.text = UiText.GameOverMessage(
                reasonLine,
                args.FinalScore,
                bestScore,
                isNewHighScore);

            _gameOverPanel?.Show();
            _gameController.LockForGameOver();
        }
    }
}
