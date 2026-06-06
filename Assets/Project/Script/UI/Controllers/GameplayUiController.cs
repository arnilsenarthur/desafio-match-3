using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.Localization;
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
        private SettingsPanelView _settingsPanel;

        [SerializeField]
        private ConfirmDialogView _confirmDialog;

        [SerializeField]
        private UiPanelView _gameOverPanel;

        [SerializeField]
        private TMP_Text _gameOverMessageText;

        [SerializeField]
        private GameplayTutorialController _gameplayTutorial;

        private bool _gameEventsBound;
        private GameEndedEventArgs _lastGameEndedArgs;
        private bool _gameOverVisible;
        private int _gameOverBestScore;
        private bool _gameOverIsNewHighScore;
        private string _gameOverReasonKey;

        private void Awake()
        {
            if (_gameController == null)
            {
                _gameController = GetComponent<GameController>();
            }

            if (_gameplayTutorial == null)
            {
                _gameplayTutorial = GetComponent<GameplayTutorialController>();
            }

            UiPanelView.HideAllOnLoad(_pausePanel, _confirmDialog, _gameOverPanel);
            _settingsPanel?.Hide(animated: false);
        }

        private void OnEnable()
        {
            LocalizationService.LanguageChanged += OnLanguageChanged;

            if (_confirmDialog != null)
            {
                _confirmDialog.Confirmed += OnConfirmDialogConfirmed;
            }

            BindGameEvents();
        }

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;

            if (_confirmDialog != null)
            {
                _confirmDialog.Confirmed -= OnConfirmDialogConfirmed;
            }

            UnbindGameEvents();
        }

        public void SkipTutorial() => _gameplayTutorial?.SkipTutorial();

        private void BindGameEvents()
        {
            UnbindGameEvents();

            if (!GameService.IsActive)
            {
                return;
            }

            GameService.GameStarted += OnGameStarted;
            GameService.GameEnded += OnGameEnded;
            _gameEventsBound = true;
        }

        private void UnbindGameEvents()
        {
            if (!_gameEventsBound)
            {
                return;
            }

            GameService.GameStarted -= OnGameStarted;
            GameService.GameEnded -= OnGameEnded;
            _gameEventsBound = false;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape) || !this)
            {
                return;
            }

            if (_settingsPanel != null && _settingsPanel.IsVisible)
            {
                _settingsPanel.Hide();
                return;
            }

            if (_confirmDialog != null && _confirmDialog.IsVisible)
            {
                _confirmDialog.Cancel();
                return;
            }

            if (_gameplayTutorial != null && _gameplayTutorial.IsActive)
            {
                SkipTutorial();
                return;
            }

            if (_gameOverPanel != null && _gameOverPanel.IsVisible)
            {
                return;
            }

            TogglePause();
        }

        public void Resume() => ApplyPause(false);

        public void ShowSettings() => _settingsPanel?.Show();

        public void CloseSettings() => _settingsPanel?.Hide();

        public void GoToMainMenu() => SceneLoader.LoadMainMenu();

        public void Retry() => GameService.RestartCurrentGame();

        public void ShowRestartConfirm() => _confirmDialog?.ShowRestartConfirm();

        public void ShowMainMenuConfirm() => _confirmDialog?.ShowMainMenuConfirm();

        public void CancelConfirm() => _confirmDialog?.Cancel();

        public void Confirm() => _confirmDialog?.Confirm();

        private void OnConfirmDialogConfirmed(ConfirmDialogView.ConfirmAction action)
        {
            switch (action)
            {
                case ConfirmDialogView.ConfirmAction.Restart:
                    ApplyPause(false);
                    GameService.RestartCurrentGame();
                    break;
                case ConfirmDialogView.ConfirmAction.MainMenu:
                    GoToMainMenu();
                    break;
            }
        }

        private void TogglePause()
        {
            if (!GameService.IsActive || GameService.IsGameOver)
            {
                return;
            }

            ApplyPause(!GameService.IsPaused);
        }

        private void ApplyPause(bool paused)
        {
            if (_gameController == null)
            {
                return;
            }

            if (paused)
            {
                _pausePanel?.Show();
            }
            else
            {
                _pausePanel?.Hide();
                _settingsPanel?.Hide();
                _confirmDialog?.Cancel();
            }

            GameService.SetPaused(paused);
        }

        private void OnGameStarted(GameStartedEventArgs args)
        {
            if (!this)
            {
                return;
            }

            _gameOverVisible = false;
            _gameOverPanel?.Hide();
            _confirmDialog?.Cancel();

            if (_gameplayTutorial == null || !_gameplayTutorial.IsActive)
            {
                ApplyPause(false);
            }
        }

        private void OnGameEnded(GameEndedEventArgs args)
        {
            if (!this || _gameController == null)
            {
                return;
            }

            _pausePanel?.Hide();
            _settingsPanel?.Hide();
            _confirmDialog?.Cancel();

            _lastGameEndedArgs = args;
            _gameOverVisible = true;
            _gameOverReasonKey = args.Reason switch
            {
                GameEndReason.TargetScoreReached => LocKeys.GameOverGoalReached,
                GameEndReason.TimeUp => LocKeys.GameOverTimeUp,
                _ => LocKeys.GameOverDefault
            };

            string difficultyId = GameRunContext.SelectedDifficultyId;
            _gameOverIsNewHighScore = HighScoreStorage.TrySetHighScore(difficultyId, args.FinalScore);
            _gameOverBestScore = HighScoreStorage.Get(difficultyId);
            ApplyGameOverText();

            _gameOverPanel?.Show();
            GameService.LockForGameOver();
        }

        private void ApplyGameOverText()
        {
            if (_gameOverMessageText == null)
            {
                return;
            }

            _gameOverMessageText.text = LocalizationService.Localize(
                LocKeys.GameOverMessage,
                LocalizationService.Localize(_gameOverReasonKey),
                _lastGameEndedArgs.FinalScore,
                _gameOverBestScore,
                _gameOverIsNewHighScore ? LocalizationService.Localize(LocKeys.GameOverNewHighScore) : string.Empty);
        }

        private void OnLanguageChanged()
        {
            if (!this)
            {
                return;
            }

            if (_gameOverVisible)
            {
                ApplyGameOverText();
            }
        }
    }
}
