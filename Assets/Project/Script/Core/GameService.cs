using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Core
{
    public class GameService
    {
        private BoardState _board;
        private BoardState _workingBoard;
        private bool[] _matchedFlags;
        private List<int> _tilesTypes;
        private List<int> _noMatchTypesScratch;
        private List<Vector2Int> _matchedPositions;
        private List<MovedTileInfo> _movedTilesList;
        private List<AddedTileInfo> _addedTilesList;
        private readonly Dictionary<int, MovedTileInfo> _movedTilesById = new();
        private int _tileCount;

        public bool IsValidMovement(int fromX, int fromY, int toX, int toY)
        {
            _board.Swap(fromX, fromY, toX, toY);
            bool valid = CreatesMatchAt(_board, fromX, fromY) ||
                         CreatesMatchAt(_board, toX, toY);
            _board.Swap(fromX, fromY, toX, toY);
            return valid;
        }

        public BoardState StartGame(int boardWidth, int boardHeight)
        {
            _tilesTypes = new List<int> { 0, 1, 2, 3 };
            EnsureBoards(boardWidth, boardHeight);
            CreateBoard(_board, _tilesTypes);

            return _board;
        }

        public List<BoardSequence> SwapTile(int fromX, int fromY, int toX, int toY)
        {
            _workingBoard.CopyFrom(_board);
            _workingBoard.Swap(fromX, fromY, toX, toY);

            List<BoardSequence> boardSequences = new();
            int matchCount = FindMatches(_workingBoard, _matchedFlags);

            while (matchCount > 0)
            {
                _matchedPositions.Clear();
                for (int y = 0; y < _workingBoard.Height; y++)
                {
                    for (int x = 0; x < _workingBoard.Width; x++)
                    {
                        if (!_matchedFlags[_workingBoard.ToIndex(x, y)])
                            continue;
                        
                        _matchedPositions.Add(new Vector2Int(x, y));
                        _workingBoard.Clear(x, y);
                    }
                }

                ApplyColumnGravity(_workingBoard);

                _addedTilesList.Clear();
                for (int y = _workingBoard.Height - 1; y > -1; y--)
                {
                    for (int x = _workingBoard.Width - 1; x > -1; x--)
                    {
                        if (_workingBoard.GetType(x, y) != -1)
                            continue;

                        int tileType = Random.Range(0, _tilesTypes.Count);
                        int id = _tileCount++;
                        _workingBoard.Set(x, y, id, _tilesTypes[tileType]);
                        _addedTilesList.Add(new AddedTileInfo
                        {
                            Position = new Vector2Int(x, y),
                            Type = _tilesTypes[tileType]
                        });
                    }
                }

                boardSequences.Add(new BoardSequence
                {
                    MatchedPosition = new List<Vector2Int>(_matchedPositions),
                    MovedTiles = new List<MovedTileInfo>(_movedTilesList),
                    AddedTiles = new List<AddedTileInfo>(_addedTilesList)
                });

                matchCount = FindMatches(_workingBoard, _matchedFlags);
            }

            _board.CopyFrom(_workingBoard);
            return boardSequences;
        }

        private void EnsureBoards(int width, int height)
        {
            if (_board != null && _board.Width == width && _board.Height == height)
            {
                return;
            }

            _board = new BoardState(width, height);
            _workingBoard = new BoardState(width, height);
            _matchedFlags = new bool[width * height];
            _matchedPositions = new List<Vector2Int>(width * height);
            _movedTilesList = new List<MovedTileInfo>(width * height);
            _addedTilesList = new List<AddedTileInfo>(width * height);
            _noMatchTypesScratch = new List<int>(_tilesTypes.Count);
        }

        private static bool CreatesMatchAt(BoardState board, int x, int y)
        {
            int type = board.GetType(x, y);
            if (type < 0)
            {
                return false;
            }

            int horizontal = 1;
            for (int i = x - 1; i >= 0 && board.GetType(i, y) == type; i--)
            {
                horizontal++;
            }

            for (int i = x + 1; i < board.Width && board.GetType(i, y) == type; i++)
            {
                horizontal++;
            }

            if (horizontal >= 3)
            {
                return true;
            }

            int vertical = 1;
            for (int i = y - 1; i >= 0 && board.GetType(x, i) == type; i--)
            {
                vertical++;
            }

            for (int i = y + 1; i < board.Height && board.GetType(x, i) == type; i++)
            {
                vertical++;
            }

            return vertical >= 3;
        }

        private void ApplyColumnGravity(BoardState board)
        {
            _movedTilesList.Clear();
            _movedTilesById.Clear();

            for (int x = 0; x < board.Width; x++)
            {
                int writeY = 0;
                for (int readY = 0; readY < board.Height; readY++)
                {
                    int type = board.GetType(x, readY);
                    if (type < 0)
                    {
                        continue;
                    }

                    int id = board.GetId(x, readY);

                    if (readY != writeY)
                    {
                        board.Set(x, writeY, id, type);
                        board.Clear(x, readY);

                        if (_movedTilesById.TryGetValue(id, out MovedTileInfo movedTileInfo))
                        {
                            movedTileInfo.To = new Vector2Int(x, writeY);
                        }
                        else
                        {
                            movedTileInfo = new MovedTileInfo
                            {
                                From = new Vector2Int(x, readY),
                                To = new Vector2Int(x, writeY)
                            };
                            _movedTilesById.Add(id, movedTileInfo);
                            _movedTilesList.Add(movedTileInfo);
                        }
                    }

                    writeY++;
                }

                for (int emptyY = writeY; emptyY < board.Height; emptyY++)
                {
                    board.Clear(x, emptyY);
                }
            }
        }

        private void CreateBoard(BoardState board, List<int> tileTypes)
        {
            _tileCount = 0;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    _noMatchTypesScratch.Clear();
                    for (int i = 0; i < tileTypes.Count; i++)
                    {
                        _noMatchTypesScratch.Add(tileTypes[i]);
                    }

                    if (x > 1 &&
                        board.GetType(x - 1, y) == board.GetType(x - 2, y))
                    {
                        _noMatchTypesScratch.Remove(board.GetType(x - 1, y));
                    }

                    if (y > 1 &&
                        board.GetType(x, y - 1) == board.GetType(x, y - 2))
                    {
                        _noMatchTypesScratch.Remove(board.GetType(x, y - 1));
                    }

                    int type = _noMatchTypesScratch[Random.Range(0, _noMatchTypesScratch.Count)];
                    board.Set(x, y, _tileCount++, type);
                }
            }
        }

        private static int FindMatches(BoardState board, bool[] matchedFlags)
        {
            int matchCount = 0;
            int cellCount = board.Width * board.Height;

            for (int i = 0; i < cellCount; i++)
            {
                matchedFlags[i] = false;
            }

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    int type = board.GetType(x, y);

                    if (x > 1 &&
                        type == board.GetType(x - 1, y) &&
                        type == board.GetType(x - 2, y))
                    {
                        matchCount += MarkMatch(board, matchedFlags, x, y);
                        matchCount += MarkMatch(board, matchedFlags, x - 1, y);
                        matchCount += MarkMatch(board, matchedFlags, x - 2, y);
                    }

                    if (y > 1 &&
                        type == board.GetType(x, y - 1) &&
                        type == board.GetType(x, y - 2))
                    {
                        matchCount += MarkMatch(board, matchedFlags, x, y);
                        matchCount += MarkMatch(board, matchedFlags, x, y - 1);
                        matchCount += MarkMatch(board, matchedFlags, x, y - 2);
                    }
                }
            }

            return matchCount;
        }

        private static int MarkMatch(BoardState board, bool[] matchedFlags, int x, int y)
        {
            int index = board.ToIndex(x, y);
            if (matchedFlags[index])
            {
                return 0;
            }

            matchedFlags[index] = true;
            return 1;
        }
    }
}
