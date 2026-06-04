using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.UI.Controllers;
using Gazeus.DesafioMatch3.UI.Views;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    [DefaultExecutionOrder(-100)]
    public class GameController : MonoBehaviour
    {
        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private GameHudView _hudView;

        [SerializeField]
        private GameConfig _gameConfig;

        [SerializeField]
        private GameplayUiController _gameplayUi;

        [SerializeField]
        private GameplayTutorialController _gameplayTutorial;

#if UNITY_EDITOR
        [SerializeField]
        private string _fallbackDifficultyId = "normal";
#endif

        private GameService _gameService;
        private bool _isAnimating;
        private bool _isPaused;
        private bool _isCountdownActive;
        private bool _interactionLocked;
        private bool _tutorialAdvancePending;
        private Coroutine _countdownCoroutine;
        private Transform _boardTweenRoot;

        public GameService GameService => _gameService;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            if (_gameplayUi == null)
            {
                _gameplayUi = GetComponent<GameplayUiController>();
            }

            if (_gameplayTutorial == null)
            {
                _gameplayTutorial = GetComponent<GameplayTutorialController>();
            }

            _boardTweenRoot = _boardView != null ? _boardView.transform : null;
            _gameService = new GameService(_gameConfig);
            _boardView.Configure(_gameConfig, _gameService.Events);
            _boardView.TileClicked += OnTileClick;
            _hudView.Bind(_gameService.Events);
        }

        private void OnDisable() => Cleanup();

        private void OnDestroy() => Cleanup();

        private void Cleanup()
        {
            StopCountdown();

            if (_boardView != null)
            {
                _boardView.TileClicked -= OnTileClick;
            }

            if (_boardTweenRoot != null)
            {
                DOTween.Kill(_boardTweenRoot, true);
            }
        }

        private void Start()
        {
            if (!TryBeginRun(ResolveDifficultyId(), startCountdown: false))
            {
                SceneLoader.LoadMainMenu();
                return;
            }

            if (TryStartTutorial())
            {
                return;
            }

            BeginMatchAfterTutorial();
        }

        private bool TryStartTutorial()
        {
            GameplayTutorialController tutorial = GetTutorialController();
            if (tutorial == null)
            {
                return false;
            }

            if (tutorial.IsActive)
            {
                return true;
            }

            return tutorial.TryBeginInteractiveTutorial();
        }

        private void BeginMatchAfterTutorial()
        {
            if (_gameplayUi != null)
            {
                _gameplayUi.BeginMatchCountdown();
            }
            else
            {
                BeginCountdown();
            }
        }

        public void BeginGameplayCountdown() => BeginCountdown();

        public void BeginTutorialSession()
        {
            _isAnimating = false;
            _interactionLocked = false;
            _tutorialAdvancePending = false;
            RefreshInteractionState();
        }

        public void RefreshTutorialBoard()
        {
            _gameService.RegenerateTutorialBaseBoard();
            _boardView.ClearSelection();
            _boardView.RebuildFromState(_gameService.Board);
            ClearTutorialGuide();
            RefreshInteractionState();
        }

        public void ApplyTutorialStep(TutorialStepDefinition step)
        {
            _gameService.ApplyTutorialStep(step);
            _boardView.ClearSelection();
            _boardView.RebuildFromState(_gameService.Board);
            RefreshInteractionState();
        }

        public void SetTutorialGuide(Vector2Int selectCell, Vector2Int swapTargetCell) =>
            _gameService.Events.RaiseTutorialGuideChanged(
                new TutorialGuideEventArgs(true, selectCell, swapTargetCell));

        public void ClearTutorialGuide() =>
            _gameService.Events.RaiseTutorialGuideChanged(TutorialGuideEventArgs.Inactive);

        public void SetInteractionLocked(bool locked)
        {
            _interactionLocked = locked;
            RefreshInteractionState();
        }

        public void FinishTutorialAndStartMatch()
        {
            _isAnimating = false;
            _interactionLocked = false;
            _tutorialAdvancePending = false;
            SetPaused(false);
            DOTween.Kill(_boardView.transform, true);
            _boardView.ClearBoard();
            ClearTutorialGuide();

            string difficultyId = ResolveDifficultyId();
            if (string.IsNullOrEmpty(difficultyId) || !TryBeginRun(difficultyId, startCountdown: true))
            {
                SceneLoader.LoadMainMenu();
            }
        }

        public void RestartCurrentGame()
        {
            if (GameRunContext.HasSelectedDifficulty)
            {
                RestartGame(GameRunContext.SelectedDifficultyId);
                return;
            }

#if UNITY_EDITOR
            if (_gameConfig.TryGetDifficulty(_fallbackDifficultyId, out _))
            {
                RestartGame(_fallbackDifficultyId);
            }
#endif
        }

        public void SetPaused(bool paused)
        {
            if (_gameService.IsGameOver)
            {
                return;
            }

            _isPaused = paused;
            SyncTimerPaused();
            RefreshInteractionState();
        }

        public void LockForGameOver()
        {
            _isPaused = true;
            SyncTimerPaused();
            RefreshInteractionState();
        }

        public void RestartGame(string difficultyId)
        {
            if (!_gameConfig.TryGetDifficulty(difficultyId, out _))
            {
                Debug.LogError($"Cannot restart game with unknown difficulty id: {difficultyId}");
                return;
            }

            _isAnimating = false;
            _interactionLocked = false;
            _tutorialAdvancePending = false;
            StopCountdown();
            SetPaused(false);
            DOTween.Kill(_boardView.transform, true);

            GameRunContext.Clear();
            GameRunContext.SelectDifficulty(difficultyId);

            _boardView.ClearBoard();
            _gameService.ExitTutorialMode();

            if (!TryBeginRun(difficultyId, startCountdown: false))
            {
                RefreshInteractionState();
                return;
            }

            if (_gameplayUi != null && _gameplayUi.TryHandleMatchReadyForTutorial())
            {
                RefreshInteractionState();
                return;
            }

            BeginCountdown();
            RefreshInteractionState();
        }

        private bool TryBeginRun(string difficultyId, bool startCountdown)
        {
            if (!_gameService.TryStart(difficultyId, out BoardState board))
            {
                return false;
            }

            _boardView.CreateBoard(board);
            if (startCountdown)
            {
                BeginCountdown();
            }

            return true;
        }

        private string ResolveDifficultyId()
        {
            if (GameRunContext.HasSelectedDifficulty &&
                _gameConfig.TryGetDifficulty(GameRunContext.SelectedDifficultyId, out _))
            {
                return GameRunContext.SelectedDifficultyId;
            }

#if UNITY_EDITOR
            if (_gameConfig != null &&
                _gameConfig.TryGetDifficulty(_fallbackDifficultyId, out _))
            {
                return _fallbackDifficultyId;
            }
#endif

            return null;
        }

        private void BeginCountdown()
        {
            StopCountdown();
            _countdownCoroutine = StartCoroutine(RunCountdown());
        }

        private void StopCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            if (!_isCountdownActive)
            {
                return;
            }

            _isCountdownActive = false;
            _gameService.Events.RaiseCountdownChanged(CountdownChangedEventArgs.Hidden);
            SyncTimerPaused();
            RefreshInteractionState();
        }

        private IEnumerator RunCountdown()
        {
            try
            {
                yield return GameplayCountdownRunner.Run(
                    _gameConfig,
                    _gameService.Events,
                    () => _isPaused,
                    active =>
                    {
                        _isCountdownActive = active;
                        SyncTimerPaused();
                        RefreshInteractionState();
                    });
            }
            finally
            {
                _countdownCoroutine = null;

                if (_isCountdownActive)
                {
                    _isCountdownActive = false;
                    _gameService.Events.RaiseCountdownChanged(CountdownChangedEventArgs.Hidden);
                    SyncTimerPaused();
                    RefreshInteractionState();
                }
            }
        }

        private void Update()
        {
            SyncTimerPaused();
            _gameService.Tick(Time.deltaTime);
        }

        private void SyncTimerPaused() =>
            _gameService.TimerPaused = _isAnimating || _isPaused || _isCountdownActive;

        private void RefreshInteractionState()
        {
            bool canInteract = !_isPaused && !_isAnimating && !_isCountdownActive && !_interactionLocked &&
                               !_gameService.IsGameOver;
            _boardView.SetInteractionEnabled(canInteract);
        }

        private void AnimateBoard(List<BoardSequence> boardSequences, Action onComplete)
        {
            if (boardSequences == null || boardSequences.Count == 0)
            {
                onComplete();
                return;
            }

            Sequence sequence = DOTween.Sequence();

            foreach (BoardSequence boardSequence in boardSequences)
            {
                sequence.Append(_boardView.DestroyTiles(boardSequence.MatchedPosition));
                sequence.Append(_boardView.MoveTiles(boardSequence.MovedTiles));
                sequence.Append(_boardView.CreateTile(boardSequence.AddedTiles));
            }

            sequence.OnComplete(() => onComplete());
        }

        private void OnTileClick(Vector2Int cell)
        {
            if (_isPaused || _isAnimating || _isCountdownActive || _gameService.IsGameOver)
            {
                return;
            }

            if (!_boardView.CanSelectTutorialCell(cell))
            {
                return;
            }

            if (!_boardView.HasSelection)
            {
                _boardView.SelectCell(cell);
                return;
            }

            if (!_boardView.TryGetSelectedCell(out Vector2Int selectedCell))
            {
                _boardView.SelectCell(cell);
                return;
            }

            if (selectedCell == cell)
            {
                _boardView.ClearSelection();
                return;
            }

            if (!BoardCell.AreAdjacent(selectedCell, cell))
            {
                if (_boardView.CanSelectTutorialCell(cell))
                {
                    _boardView.SelectCell(cell);
                }

                return;
            }

            GameplayTutorialController tutorial = GetTutorialController();
            bool tutorialPractice = tutorial != null && tutorial.IsActive && !tutorial.IsShowingIntro;

            bool isValid = tutorialPractice
                ? _gameService.IsTutorialSwapValid(selectedCell, cell)
                : _gameService.IsValidMovement(selectedCell, cell);

            _isAnimating = true;
            _boardView.ClearSelection(keepTutorialSwapHint: tutorialPractice);
            SyncTimerPaused();
            RefreshInteractionState();

            if (isValid && tutorialPractice)
            {
                _tutorialAdvancePending = true;
            }

            _boardView.SwapTiles(selectedCell, cell).onComplete += () =>
            {
                if (isValid)
                {
                    AnimateBoard(_gameService.ResolveValidSwap(selectedCell, cell), OnSwapAnimationComplete);
                }
                else
                {
                    _boardView.SwapTiles(cell, selectedCell).onComplete += OnSwapAnimationComplete;
                }
            };
        }

        private void OnSwapAnimationComplete()
        {
            if (_gameService.IsTutorialMode)
            {
                if (_tutorialAdvancePending)
                {
                    _tutorialAdvancePending = false;
                    GetTutorialController()?.OnPracticeSwapCompleted();
                }
            }
            else if (_gameService.TryRegenerateBoardIfNoValidMoves())
            {
                _boardView.SyncFromState(_gameService.Board);
            }

            _isAnimating = false;
            SyncTimerPaused();
            RefreshInteractionState();
        }

        private GameplayTutorialController GetTutorialController() =>
            _gameplayTutorial != null ? _gameplayTutorial : GetComponent<GameplayTutorialController>();
    }
}
