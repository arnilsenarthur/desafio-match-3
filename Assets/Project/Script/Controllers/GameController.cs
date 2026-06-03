using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Core;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.ScriptableObjects;
using Gazeus.DesafioMatch3.Views;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private GameHudView _hudView;
        [SerializeField] private GameConfig _gameConfig;

#if UNITY_EDITOR
        [SerializeField] private string _fallbackDifficultyId = "normal";
#endif

        private GameService _gameService;
        private bool _isAnimating;
        private bool _isPaused;

        public GameService GameService => _gameService;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            _boardView.Configure(_gameConfig);
            _gameService = new GameService(_gameConfig);
            _boardView.TileClicked += OnTileClick;
            _hudView.Bind(_gameService.Events);
        }

        private void OnDestroy()
        {
            _boardView.TileClicked -= OnTileClick;
        }

        private void Start()
        {
            string difficultyId = ResolveDifficultyId();
            if (difficultyId == null)
            {
                SceneLoader.LoadMainMenu();
                return;
            }

            BoardState board = _gameService.Start(difficultyId);
            _boardView.CreateBoard(board);
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

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
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
            SetPaused(false);
            DOTween.Kill(_boardView.transform, true);

            GameRunContext.Clear();
            GameRunContext.SelectDifficulty(difficultyId);

            _boardView.ClearBoard();
            BoardState board = _gameService.Start(difficultyId);
            _boardView.CreateBoard(board);
            RefreshInteractionState();
        }

        private void Update()
        {
            _gameService.TimerPaused = _isAnimating || _isPaused;
            _gameService.Tick(Time.deltaTime);
        }

        private void RefreshInteractionState()
        {
            bool canInteract = !_isPaused && !_isAnimating && !_gameService.IsGameOver;
            _boardView.SetInteractionEnabled(canInteract);
        }

        private void AnimateBoard(List<BoardSequence> boardSequences, Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();

            foreach (BoardSequence boardSequence in boardSequences)
            {
                sequence.Append(_boardView.DestroyTiles(boardSequence.MatchedPosition));
                sequence.Append(_boardView.MoveTiles(boardSequence.MovedTiles));
                sequence.Append(_boardView.CreateTile(boardSequence.AddedTiles));
            }

            sequence.onComplete += () => onComplete();
        }

        private void OnTileClick(int x, int y)
        {
            if (_isPaused || _isAnimating || _gameService.IsGameOver)
            {
                return;
            }

            if (!_boardView.HasSelection)
            {
                _boardView.SelectCell(x, y);
                return;
            }

            if (!_boardView.TryGetSelectedCell(out int fromX, out int fromY))
            {
                _boardView.SelectCell(x, y);
                return;
            }

            if (fromX == x && fromY == y)
            {
                _boardView.ClearSelection();
                return;
            }

            if (Mathf.Abs(fromX - x) + Mathf.Abs(fromY - y) > 1)
            {
                _boardView.SelectCell(x, y);
                return;
            }

            bool isValid = _gameService.IsValidMovement(fromX, fromY, x, y);

            _isAnimating = true;
            _boardView.ClearSelection();
            RefreshInteractionState();

            _boardView.SwapTiles(fromX, fromY, x, y).onComplete += () =>
            {
                if (isValid)
                {
                    List<BoardSequence> swapResult = _gameService.ResolveValidSwap(fromX, fromY, x, y);
                    AnimateBoard(swapResult, OnSwapAnimationComplete);
                }
                else
                {
                    _boardView.SwapTiles(x, y, fromX, fromY).onComplete += OnSwapAnimationComplete;
                }
            };
        }

        private void OnSwapAnimationComplete()
        {
            if (_gameService.TryRegenerateBoardIfNoValidMoves())
            {
                _boardView.SyncFromState(_gameService.Board);
            }

            _isAnimating = false;
            RefreshInteractionState();
        }
    }
}
