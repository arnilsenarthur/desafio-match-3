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
        private SettingsPanelController _settingsPanel;

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

        [SerializeField]
        private GameplayTutorialController _gameplayTutorial;

        private bool _gameEventsBound;
        private ConfirmAction _pendingConfirmAction;
        private ConfirmDialogContent _activeConfirm;
        private bool _hasActiveConfirm;
        private GameEndedEventArgs _lastGameEndedArgs;
        private bool _gameOverVisible;
        private int _gameOverBestScore;
        private bool _gameOverIsNewHighScore;
        private string _gameOverReasonKey;

        private enum ConfirmAction
        {
            None,
            Restart,
            MainMenu
        }

        private readonly struct ConfirmDialogContent
        {
            public ConfirmAction Action { get; }
            public string TitleKey { get; }
            public string MessageKey { get; }
            public string ConfirmLabelKey { get; }

            public ConfirmDialogContent(
                ConfirmAction action,
                string titleKey,
                string messageKey,
                string confirmLabelKey)
            {
                Action = action;
                TitleKey = titleKey;
                MessageKey = messageKey;
                ConfirmLabelKey = confirmLabelKey;
            }
        }

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

            _pausePanel?.Hide();
            _settingsPanel?.Hide();
            _confirmPanel?.Hide();
            _gameOverPanel?.Hide();
        }

        private void OnEnable()
        {
            LocalizationService.LanguageChanged += OnLanguageChanged;
            BindGameEvents();
        }

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
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

            if (_confirmPanel != null && _confirmPanel.gameObject.activeSelf)
            {
                CancelConfirm();
                return;
            }

            if (_gameplayTutorial != null && _gameplayTutorial.IsActive)
            {
                SkipTutorial();
                return;
            }

            if (_gameOverPanel != null && _gameOverPanel.gameObject.activeSelf)
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

        public void ShowRestartConfirm() => ShowConfirm(new ConfirmDialogContent(
            ConfirmAction.Restart,
            LocKeys.ConfirmRestartTitle,
            LocKeys.ConfirmRestartMessage,
            LocKeys.ConfirmRestartButton));

        public void ShowMainMenuConfirm() => ShowConfirm(new ConfirmDialogContent(
            ConfirmAction.MainMenu,
            LocKeys.ConfirmMainMenuTitle,
            LocKeys.ConfirmMainMenuMessage,
            LocKeys.ConfirmMainMenuButton));

        public void CancelConfirm()
        {
            _pendingConfirmAction = ConfirmAction.None;
            _hasActiveConfirm = false;
            _confirmPanel?.Hide();
        }

        public void Confirm()
        {
            ConfirmAction action = _pendingConfirmAction;
            _pendingConfirmAction = ConfirmAction.None;
            _hasActiveConfirm = false;
            _confirmPanel?.Hide();

            switch (action)
            {
                case ConfirmAction.Restart:
                    ApplyPause(false);
                    GameService.RestartCurrentGame();
                    break;
                case ConfirmAction.MainMenu:
                    GoToMainMenu();
                    break;
            }
        }

        private void ShowConfirm(ConfirmDialogContent content)
        {
            _pendingConfirmAction = content.Action;
            _activeConfirm = content;
            _hasActiveConfirm = true;
            ApplyConfirmText();
            _confirmPanel?.Show();
        }

        private void ApplyConfirmText()
        {
            if (!_hasActiveConfirm)
            {
                return;
            }

            if (_confirmTitleText != null)
            {
                _confirmTitleText.text = LocalizationService.Localize(_activeConfirm.TitleKey);
            }

            if (_confirmMessageText != null)
            {
                _confirmMessageText.text = LocalizationService.Localize(_activeConfirm.MessageKey);
            }

            if (_confirmButtonText != null)
            {
                _confirmButtonText.text = LocalizationService.Localize(_activeConfirm.ConfirmLabelKey);
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
                CancelConfirm();
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
            CancelConfirm();
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
            CancelConfirm();

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

            if (_confirmPanel != null && _confirmPanel.gameObject.activeSelf)
            {
                ApplyConfirmText();
            }

            if (_gameOverVisible)
            {
                ApplyGameOverText();
            }
        }
    }
}
