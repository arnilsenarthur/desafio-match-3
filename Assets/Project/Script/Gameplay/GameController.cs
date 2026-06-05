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
        private GameplayTutorialController _gameplayTutorial;

#if UNITY_EDITOR
        [SerializeField]
        private string _fallbackDifficultyId = "normal";
#endif

        private bool _isAnimating;
        private bool _isPaused;
        private bool _isCountdownActive;
        private bool _interactionLocked;
        private bool _tutorialAdvancePending;
        private bool _wired;
        private bool _sessionStarted;
        private Coroutine _countdownCoroutine;
        private Transform _boardTweenRoot;

        public GameConfig Config => _gameConfig;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            if (_gameplayTutorial == null)
            {
                _gameplayTutorial = GetComponent<GameplayTutorialController>();
            }

            _boardTweenRoot = _boardView != null ? _boardView.transform : null;
        }

        private void OnEnable()
        {
            if (!Wire())
            {
                Debug.LogError("GameController could not wire gameplay systems.", this);
            }
        }

        private void OnDisable() => StopRunningWork();

        private void OnDestroy() => Unwire();

        private void Start() => TryStartSession();

        private bool Wire()
        {
            if (_wired)
            {
                return GameService.IsActive && _boardView != null && _boardView.IsReady;
            }

            if (_gameConfig == null || _boardView == null || _hudView == null)
            {
                return false;
            }

            GameService.Attach(this);
            GameService.BeginSession(_gameConfig);

            if (!_boardView.Configure(_gameConfig))
            {
                GameService.EndSession();
                GameService.Detach(this);
                return false;
            }

            _boardView.TileClicked += OnTileClick;
            _hudView.Bind();
            _wired = true;
            return true;
        }

        private void Unwire()
        {
            StopRunningWork();

            if (_boardView != null)
            {
                _boardView.TileClicked -= OnTileClick;
            }

            if (_hudView != null)
            {
                _hudView.Unbind();
            }

            if (_boardTweenRoot != null)
            {
                DOTween.Kill(_boardTweenRoot, true);
            }

            GameService.EndSession();
            GameService.Detach(this);
            _wired = false;
            _sessionStarted = false;
        }

        private void StopRunningWork()
        {
            StopCountdown();
            _isAnimating = false;
            _tutorialAdvancePending = false;

            if (_boardTweenRoot != null)
            {
                DOTween.Kill(_boardTweenRoot, true);
            }

            if (_boardView != null)
            {
                _boardView.CancelRunningAnimations();
            }

            SyncTimerPaused();
            RefreshInteractionState();
        }

        private void TryStartSession()
        {
            if (_sessionStarted || !_wired || !isActiveAndEnabled)
            {
                return;
            }

            _sessionStarted = true;

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

        private void BeginMatchAfterTutorial() => BeginCountdown();

        public void BeginTutorialSession()
        {
            _isAnimating = false;
            _tutorialAdvancePending = false;
            RefreshInteractionState();
        }

        public void RefreshTutorialBoard()
        {
            if (!EnsureGameReady())
            {
                return;
            }

            GameService.RegenerateTutorialBaseBoard();
            _boardView.ClearSelection();
            _boardView.RebuildFromState(GameService.Board);
            ClearTutorialGuide();
            RefreshInteractionState();
        }

        public void ApplyTutorialStep(TutorialStepDefinition step)
        {
            if (!EnsureGameReady())
            {
                return;
            }

            GameService.ApplyTutorialStep(step);
            _boardView.ClearSelection();
            _boardView.RebuildFromState(GameService.Board);
            RefreshInteractionState();
        }

        public void SetTutorialGuide(Vector2Int selectCell, Vector2Int swapTargetCell)
        {
            if (!GameService.IsActive)
            {
                return;
            }

            GameService.NotifyTutorialGuideChanged(
                new TutorialGuideEventArgs(true, selectCell, swapTargetCell));
        }

        public void ClearTutorialGuide()
        {
            if (GameService.IsActive)
            {
                GameService.NotifyTutorialGuideChanged(TutorialGuideEventArgs.Inactive);
            }
        }

        public void SetInteractionLocked(bool locked)
        {
            _interactionLocked = locked;
            RefreshInteractionState();
        }

        public void FinishTutorialAndStartMatch()
        {
            if (_boardView == null)
            {
                return;
            }

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
            if (_gameConfig != null && _gameConfig.TryGetDifficulty(_fallbackDifficultyId, out _))
            {
                RestartGame(_fallbackDifficultyId);
            }
#endif
        }

        public void SetPaused(bool paused)
        {
            if (!GameService.IsActive || GameService.IsGameOver)
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
            if (_gameConfig == null || !_gameConfig.TryGetDifficulty(difficultyId, out _))
            {
                Debug.LogError($"Cannot restart game with unknown difficulty id: {difficultyId}");
                return;
            }

            _isAnimating = false;
            _interactionLocked = false;
            _tutorialAdvancePending = false;
            StopCountdown();
            SetPaused(false);

            if (_boardView != null)
            {
                DOTween.Kill(_boardView.transform, true);
                _boardView.ClearBoard();
            }

            GameRunContext.Clear();
            GameRunContext.SelectDifficulty(difficultyId);

            if (GameService.IsActive)
            {
                GameService.ExitTutorialMode();
            }

            if (!TryBeginRun(difficultyId, startCountdown: false))
            {
                RefreshInteractionState();
                return;
            }

            if (TryStartTutorial())
            {
                RefreshInteractionState();
                return;
            }

            BeginCountdown();
            RefreshInteractionState();
        }

        private bool TryBeginRun(string difficultyId, bool startCountdown)
        {
            if (!EnsureGameReady() || !_boardView.IsReady ||
                !GameService.TryStart(difficultyId, out BoardState board))
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

        private bool EnsureGameReady()
        {
            if (_wired && GameService.IsActive && _boardView != null && _boardView.IsReady)
            {
                return true;
            }

            return Wire();
        }

        private string ResolveDifficultyId()
        {
            if (_gameConfig == null)
            {
                return null;
            }

            if (GameRunContext.HasSelectedDifficulty &&
                _gameConfig.TryGetDifficulty(GameRunContext.SelectedDifficultyId, out _))
            {
                return GameRunContext.SelectedDifficultyId;
            }

#if UNITY_EDITOR
            if (_gameConfig.TryGetDifficulty(_fallbackDifficultyId, out _))
            {
                return _fallbackDifficultyId;
            }
#endif

            return null;
        }

        private void BeginCountdown()
        {
            if (!isActiveAndEnabled || !EnsureGameReady())
            {
                return;
            }

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

            if (GameService.IsActive)
            {
                GameService.NotifyCountdownChanged(CountdownChangedEventArgs.Hidden);
            }

            SyncTimerPaused();
            RefreshInteractionState();
        }

        private IEnumerator RunCountdown()
        {
            try
            {
                yield return GameplayCountdownRunner.Run(_gameConfig, SetCountdownActive, this);
            }
            finally
            {
                _countdownCoroutine = null;

                if (_isCountdownActive)
                {
                    SetCountdownActive(false);
                }
            }
        }

        private void SetCountdownActive(bool active)
        {
            if (!this)
            {
                return;
            }

            _isCountdownActive = active;
            SyncTimerPaused();
            RefreshInteractionState();
        }

        private void Update()
        {
            if (!GameService.IsActive)
            {
                return;
            }

            SyncTimerPaused();
            GameService.Tick(Time.deltaTime);
        }

        private void SyncTimerPaused()
        {
            if (!GameService.IsActive)
            {
                return;
            }

            GameService.TimerPaused = _isAnimating || _isPaused || _isCountdownActive;
        }

        private void RefreshInteractionState()
        {
            if (_boardView == null || !GameService.IsActive)
            {
                return;
            }

            bool canInteract = !_isPaused && !_isAnimating && !_isCountdownActive && !_interactionLocked &&
                               !GameService.IsGameOver;
            _boardView.SetInteractionEnabled(canInteract);
        }

        private void AnimateBoard(List<BoardSequence> boardSequences, Action onComplete)
        {
            if (!this)
            {
                return;
            }

            if (boardSequences == null || boardSequences.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            foreach (BoardSequence boardSequence in boardSequences)
            {
                sequence.Append(_boardView.DestroyTiles(boardSequence.MatchedPosition));
                sequence.Append(_boardView.MoveTiles(boardSequence.MovedTiles));
                sequence.Append(_boardView.CreateTile(boardSequence.AddedTiles));
            }

            sequence.OnComplete(() =>
            {
                if (this)
                {
                    onComplete?.Invoke();
                }
            });
        }

        private void OnTileClick(Vector2Int cell)
        {
            if (!GameService.IsActive || _interactionLocked || _isPaused || _isAnimating || _isCountdownActive ||
                GameService.IsGameOver)
            {
                return;
            }

            GameplayTutorialController tutorial = GetTutorialController();
            if (tutorial != null && tutorial.IsActive && tutorial.IsShowingIntro)
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

            bool tutorialPractice = tutorial != null && tutorial.IsActive && !tutorial.IsShowingIntro;

            bool isValid = tutorialPractice
                ? GameService.IsTutorialSwapValid(selectedCell, cell)
                : GameService.IsValidMovement(selectedCell, cell);

            _isAnimating = true;
            _boardView.ClearSelection(keepTutorialSwapHint: tutorialPractice);
            SyncTimerPaused();
            RefreshInteractionState();

            if (isValid && tutorialPractice)
            {
                _tutorialAdvancePending = true;
            }

            Tween swapTween = _boardView.SwapTiles(selectedCell, cell);
            swapTween.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            swapTween.onComplete += () =>
            {
                if (!this || !GameService.IsActive)
                {
                    return;
                }

                if (isValid)
                {
                    AnimateBoard(GameService.ResolveValidSwap(selectedCell, cell), OnSwapAnimationComplete);
                }
                else
                {
                    Tween revertTween = _boardView.SwapTiles(cell, selectedCell);
                    revertTween.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                    revertTween.onComplete += OnSwapAnimationComplete;
                }
            };
        }

        private void OnSwapAnimationComplete()
        {
            if (!this || !GameService.IsActive)
            {
                return;
            }

            if (GameService.IsTutorialMode)
            {
                if (_tutorialAdvancePending)
                {
                    _tutorialAdvancePending = false;
                    GetTutorialController()?.OnPracticeSwapCompleted();
                }
            }
            else if (GameService.TryRegenerateBoardIfNoValidMoves())
            {
                _boardView.SyncFromState(GameService.Board);
            }

            _isAnimating = false;
            SyncTimerPaused();
            RefreshInteractionState();
        }

        private GameplayTutorialController GetTutorialController() =>
            _gameplayTutorial != null ? _gameplayTutorial : GetComponent<GameplayTutorialController>();
    }
}
