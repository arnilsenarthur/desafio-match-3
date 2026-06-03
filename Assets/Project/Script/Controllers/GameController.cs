using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Core;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Views;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] 
        private BoardView _boardView;
        [SerializeField] 
        private int _boardHeight = 10;
        [SerializeField] 
        private int _boardWidth = 10;

        private GameService _gameService;
        private bool _isAnimating;
        private int _selectedX = -1;
        private int _selectedY = -1;

        #region Unity Callbacks
        private void Awake()
        {
            _gameService = new GameService();
            _boardView.TileClicked += OnTileClick;
        }

        private void OnDestroy()
        {
            _boardView.TileClicked -= OnTileClick;
        }

        private void Start()
        {
            BoardState board = _gameService.StartGame(_boardWidth, _boardHeight);
            _boardView.CreateBoard(board);
        }
        #endregion

        private void AnimateBoard(List<BoardSequence> boardSequences, Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();

            foreach (var boardSequence in boardSequences)
            {
                sequence.Append(_boardView.DestroyTiles(boardSequence.MatchedPosition));
                sequence.Append(_boardView.MoveTiles(boardSequence.MovedTiles));
                sequence.Append(_boardView.CreateTile(boardSequence.AddedTiles));
            }

            sequence.onComplete += () => onComplete();
        }

        private void OnTileClick(int x, int y)
        {
            if (_isAnimating)
            {
                return;
            }

            if (_selectedX > -1 && _selectedY > -1)
            {
                if (Mathf.Abs(_selectedX - x) + Mathf.Abs(_selectedY - y) > 1)
                {
                    _selectedX = -1;
                    _selectedY = -1;
                }
                else
                {
                    int fromX = _selectedX;
                    int fromY = _selectedY;
                    bool isValid = _gameService.IsValidMovement(fromX, fromY, x, y);

                    _isAnimating = true;
                    _selectedX = -1;
                    _selectedY = -1;

                    _boardView.SwapTiles(fromX, fromY, x, y).onComplete += () =>
                    {
                        if (isValid)
                        {
                            List<BoardSequence> swapResult = _gameService.SwapTile(fromX, fromY, x, y);
                            AnimateBoard(swapResult, () => _isAnimating = false);
                        }
                        else
                        {
                            _boardView.SwapTiles(x, y, fromX, fromY).onComplete += () => _isAnimating = false;
                        }
                    };
                }
            }
            else
            {
                _selectedX = x;
                _selectedY = y;
            }
        }
    }
}
