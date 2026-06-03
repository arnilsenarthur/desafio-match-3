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

        private GameService _gameService;
        private bool _isAnimating;

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
            BoardState board = _gameService.Start();
            _boardView.CreateBoard(board);
        }

        private void Update()
        {
            _gameService.TimerPaused = _isAnimating;
            _gameService.Tick(Time.deltaTime);
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
            if (_isAnimating || _gameService.IsGameOver)
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
            _boardView.SetInteractionEnabled(false);

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
            _boardView.SetInteractionEnabled(true);
        }
    }
}
