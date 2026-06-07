using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Audio;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.Localization;
using Gazeus.DesafioMatch3.UI.Views;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Controllers
{
    [DefaultExecutionOrder(0)]
    public class GameplayUiController : MonoBehaviour
    {
        [SerializeField]
        private GameController _gameController;

        [SerializeField]
        private PausePanelView _pausePanel;

        [SerializeField]
        private SettingsPanelView _settingsPanel;

        [SerializeField]
        private ConfirmDialogView _confirmDialog;

        [SerializeField]
        private UiPanelView _gameOverPanel;

        [SerializeField]
        private GameOverView _gameOverView;

        [SerializeField]
        private GameplayTutorialController _gameplayTutorial;

        private bool _gameEventsBound;
        private bool _gameOverVisible;

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

            ResolveGameOverView();
            ResolvePanelReferences();

            UiPanelView.HideAllOnLoad(_pausePanel, _confirmDialog, _gameOverPanel);
            _settingsPanel?.Hide(animated: false);
            _gameOverView?.Hide();
        }

        private void ResolvePanelReferences()
        {
            if (_pausePanel == null)
            {
                _pausePanel = GetComponentInChildren<PausePanelView>(true);
            }

            if (_settingsPanel == null)
            {
                _settingsPanel = GetComponentInChildren<SettingsPanelView>(true);
            }

            if (_confirmDialog == null)
            {
                _confirmDialog = GetComponentInChildren<ConfirmDialogView>(true);
            }

            if (_gameOverPanel == null)
            {
                ResolveGameOverView();
                if (_gameOverView != null)
                {
                    _gameOverPanel = _gameOverView.GetComponentInParent<UiPanelView>();
                }
            }
        }

        private void ShowPanel(UiPanelView panel)
        {
            if (panel == null)
            {
                ResolvePanelReferences();
            }

            panel?.Show();
        }

        private void HidePanel(UiPanelView panel, bool animated = true)
        {
            if (panel == null)
            {
                ResolvePanelReferences();
            }

            panel?.Hide(animated);
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

        public void BindGameEvents()
        {
            UnbindGameEvents();

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
                AudioService.PlaySfx(AudioKeys.UiClick);
                HidePanel(_settingsPanel);
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

        public void Pause() => ApplyPause(true);

        public void Resume() => ApplyPause(false);

        public void ShowSettings() => ShowPanel(_settingsPanel);

        public void CloseSettings() => HidePanel(_settingsPanel);

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
                ShowPanel(_pausePanel);
            }
            else
            {
                HidePanel(_pausePanel);
                HidePanel(_settingsPanel);
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
            _gameOverView?.Hide();
            HidePanel(_gameOverPanel);
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

            HidePanel(_pausePanel);
            HidePanel(_settingsPanel);
            _confirmDialog?.Cancel();

            _gameOverVisible = true;
            ResolveGameOverView();

            string reasonKey = args.Reason switch
            {
                GameEndReason.TargetScoreReached => LocKeys.GameOverGoalReached,
                GameEndReason.TimeUp => LocKeys.GameOverTimeUp,
                _ => LocKeys.GameOverDefault
            };

            string difficultyId = GameRunContext.SelectedDifficultyId;
            bool isNewHighScore = args.IsNewHighScore;
            int bestScore = HighScoreStorage.Get(difficultyId);

            _gameOverView?.Show(reasonKey, args.FinalScore, bestScore, isNewHighScore);
            ShowPanel(_gameOverPanel);
            GameService.LockForGameOver();
        }

        private void ResolveGameOverView()
        {
            if (_gameOverView != null || _gameOverPanel == null)
            {
                return;
            }

            _gameOverView = _gameOverPanel.GetComponentInChildren<GameOverView>(true);
        }

        private void OnLanguageChanged()
        {
            if (!this || !_gameOverVisible)
            {
                return;
            }

            _gameOverView?.RefreshLanguage();
        }
    }
}
