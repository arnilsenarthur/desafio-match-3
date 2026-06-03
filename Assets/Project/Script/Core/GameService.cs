using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.ScriptableObjects;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Core
{
    public class GameService
    {
        private readonly GameConfig _config;

        private BoardState _board;
        private BoardState _workingBoard;
        private bool[] _matchedFlags;
        private List<int> _tilesTypes;
        private List<int> _noMatchTypesScratch;
        private List<Vector2Int> _matchedPositions;
        private List<int> _clearedRowsScratch;
        private List<int> _clearedColumnsScratch;
        private List<MovedTileInfo> _movedTilesList;
        private List<AddedTileInfo> _addedTilesList;
        private readonly Dictionary<int, MovedTileInfo> _movedTilesById = new();
        private int _tileCount;

        public GameEvents Events { get; } = new();
        public bool IsGameOver { get; private set; }
        public bool TimerPaused { get; set; }
        public float TimeRemaining { get; private set; }
        public int Score { get; private set; }
        public BoardState Board => _board;

        public GameService(GameConfig config)
        {
            _config = config;
        }

        public BoardState Start()
        {
            IsGameOver = false;
            Score = 0;
            TimeRemaining = _config.StartingTimeSeconds;

            InitializeBoard(_config.BoardWidth, _config.BoardHeight);
            Events.RaiseGameStarted(new GameStartedEventArgs(TimeRemaining, _config.TargetScore));
            Events.RaiseScoreChanged(new ScoreChangedEventArgs(Score, 0));
            Events.RaiseTimeChanged(new TimeChangedEventArgs(TimeRemaining, 0));

            return _board;
        }

        public void Tick(float deltaTime)
        {
            if (IsGameOver || TimerPaused || deltaTime <= 0f)
            {
                return;
            }

            float previous = TimeRemaining;
            TimeRemaining = Mathf.Max(0f, TimeRemaining - deltaTime);
            Events.RaiseTimeChanged(new TimeChangedEventArgs(TimeRemaining, TimeRemaining - previous));

            if (TimeRemaining <= 0f)
            {
                EndGame(GameEndReason.TimeUp);
            }
        }

        public bool IsValidMovement(int fromX, int fromY, int toX, int toY)
        {
            if (IsGameOver)
            {
                return false;
            }

            return WouldCreateMatchAfterSwap(_board, fromX, fromY, toX, toY);
        }

        public bool HasValidMovement()
        {
            for (int y = 0; y < _board.Height; y++)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    if (x < _board.Width - 1 &&
                        WouldCreateMatchAfterSwap(_board, x, y, x + 1, y))
                    {
                        return true;
                    }

                    if (y < _board.Height - 1 &&
                        WouldCreateMatchAfterSwap(_board, x, y, x, y + 1))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<BoardSequence> ResolveValidSwap(int fromX, int fromY, int toX, int toY)
        {
            Events.RaiseSwapStarted(new SwapStartedEventArgs(fromX, fromY, toX, toY));

            float timeBefore = TimeRemaining;
            TimeRemaining += _config.TimeBonusPerValidSwap;
            Events.RaiseTimeChanged(new TimeChangedEventArgs(TimeRemaining, TimeRemaining - timeBefore));

            List<BoardSequence> sequences = SwapTile(fromX, fromY, toX, toY);
            int totalScoreDelta = 0;

            for (int i = 0; i < sequences.Count; i++)
            {
                BoardSequence sequence = sequences[i];
                sequence.ComboIndex = i;
                sequence.ScoreDelta = CalculateSequenceScore(sequence, i);
                totalScoreDelta += sequence.ScoreDelta;

                Score += sequence.ScoreDelta;
                Events.RaiseCascadeStep(new CascadeStepEventArgs(sequence, i, sequence.ScoreDelta));
                Events.RaiseScoreChanged(new ScoreChangedEventArgs(Score, sequence.ScoreDelta));
            }

            Events.RaiseSwapCompleted(new SwapCompletedEventArgs(sequences, totalScoreDelta));

            if (_config.TargetScore > 0 && Score >= _config.TargetScore)
            {
                EndGame(GameEndReason.TargetScoreReached);
            }

            return sequences;
        }

        public bool TryRegenerateBoardIfNoValidMoves()
        {
            if (HasValidMovement())
            {
                return false;
            }

            RegenerateBoard();
            Events.RaiseBoardRegenerated(new BoardRegeneratedEventArgs(_board));
            return true;
        }

        private static bool WouldCreateMatchAfterSwap(BoardState board, int fromX, int fromY, int toX, int toY)
        {
            board.Swap(fromX, fromY, toX, toY);
            bool valid = CreatesMatchAt(board, fromX, fromY) ||
                         CreatesMatchAt(board, toX, toY);
            board.Swap(fromX, fromY, toX, toY);
            return valid;
        }

        private void InitializeBoard(int boardWidth, int boardHeight)
        {
            _tilesTypes = BuildTileTypes(_config.TileTypeCount);
            EnsureBoards(boardWidth, boardHeight);
            RegenerateBoard();
        }

        public void RegenerateBoard()
        {
            const int maxAttempts = 50;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                CreateBoard(_board, _tilesTypes);

                if (HasValidMovement() && !HasImmediateMatches(_board))
                {
                    return;
                }
            }

            CreateBoard(_board, _tilesTypes);
        }

        private List<BoardSequence> SwapTile(int fromX, int fromY, int toX, int toY)
        {
            _workingBoard.CopyFrom(_board);
            _workingBoard.Swap(fromX, fromY, toX, toY);

            List<BoardSequence> boardSequences = new();
            bool hasMatches = FindMatches(_workingBoard, _matchedFlags, _clearedRowsScratch, _clearedColumnsScratch);

            while (hasMatches)
            {
                _matchedPositions.Clear();
                for (int y = 0; y < _workingBoard.Height; y++)
                {
                    for (int x = 0; x < _workingBoard.Width; x++)
                    {
                        if (!_matchedFlags[_workingBoard.ToIndex(x, y)])
                        {
                            continue;
                        }

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
                        {
                            continue;
                        }

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
                    AddedTiles = new List<AddedTileInfo>(_addedTilesList),
                    ClearedRows = new List<int>(_clearedRowsScratch),
                    ClearedColumns = new List<int>(_clearedColumnsScratch)
                });

                hasMatches = FindMatches(_workingBoard, _matchedFlags, _clearedRowsScratch, _clearedColumnsScratch);
            }

            _board.CopyFrom(_workingBoard);
            return boardSequences;
        }

        private static List<int> BuildTileTypes(int tileTypeCount)
        {
            List<int> types = new(tileTypeCount);
            for (int i = 0; i < tileTypeCount; i++)
            {
                types.Add(i);
            }

            return types;
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
            _clearedRowsScratch = new List<int>();
            _clearedColumnsScratch = new List<int>();
            _movedTilesList = new List<MovedTileInfo>(width * height);
            _addedTilesList = new List<AddedTileInfo>(width * height);
            _noMatchTypesScratch = new List<int>(_tilesTypes?.Count ?? 4);
        }

        private static bool HasImmediateMatches(BoardState board)
        {
            bool[] flags = new bool[board.Width * board.Height];
            List<int> rows = new();
            List<int> columns = new();
            return FindMatches(board, flags, rows, columns);
        }

        private static bool CreatesMatchAt(BoardState board, int x, int y)
        {
            int type = board.GetType(x, y);
            if (type < 0)
            {
                return false;
            }

            int horizontal = CountRun(board, x, y, 1, 0);
            if (horizontal >= 3)
            {
                return true;
            }

            int vertical = CountRun(board, x, y, 0, 1);
            return vertical >= 3;
        }

        private static int CountRun(BoardState board, int x, int y, int dx, int dy)
        {
            int type = board.GetType(x, y);
            int count = 1;

            for (int i = x - dx, j = y - dy;
                 i >= 0 && j >= 0 && i < board.Width && j < board.Height && board.GetType(i, j) == type;
                 i -= dx, j -= dy)
            {
                count++;
            }

            for (int i = x + dx, j = y + dy;
                 i >= 0 && j >= 0 && i < board.Width && j < board.Height && board.GetType(i, j) == type;
                 i += dx, j += dy)
            {
                count++;
            }

            return count;
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

        private static bool FindMatches(
            BoardState board,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns)
        {
            int cellCount = board.Width * board.Height;
            for (int i = 0; i < cellCount; i++)
            {
                matchedFlags[i] = false;
            }

            clearedRows.Clear();
            clearedColumns.Clear();

            ScanHorizontalRuns(board, matchedFlags, clearedRows);
            ScanVerticalRuns(board, matchedFlags, clearedColumns);

            ApplyLineClears(board, matchedFlags, clearedRows, clearedColumns);

            for (int i = 0; i < cellCount; i++)
            {
                if (matchedFlags[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static void ScanHorizontalRuns(BoardState board, bool[] matchedFlags, List<int> clearedRows)
        {
            for (int y = 0; y < board.Height; y++)
            {
                int x = 0;
                while (x < board.Width)
                {
                    int type = board.GetType(x, y);
                    if (type < 0)
                    {
                        x++;
                        continue;
                    }

                    int startX = x;
                    while (x < board.Width && board.GetType(x, y) == type)
                    {
                        x++;
                    }

                    int length = x - startX;
                    if (length < 3)
                    {
                        continue;
                    }

                    for (int i = startX; i < x; i++)
                    {
                        MarkMatch(board, matchedFlags, i, y);
                    }

                    if (length >= 4)
                    {
                        AddUnique(clearedRows, y);
                    }
                }
            }
        }

        private static void ScanVerticalRuns(BoardState board, bool[] matchedFlags, List<int> clearedColumns)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int y = 0;
                while (y < board.Height)
                {
                    int type = board.GetType(x, y);
                    if (type < 0)
                    {
                        y++;
                        continue;
                    }

                    int startY = y;
                    while (y < board.Height && board.GetType(x, y) == type)
                    {
                        y++;
                    }

                    int length = y - startY;
                    if (length < 3)
                    {
                        continue;
                    }

                    for (int i = startY; i < y; i++)
                    {
                        MarkMatch(board, matchedFlags, x, i);
                    }

                    if (length >= 4)
                    {
                        AddUnique(clearedColumns, x);
                    }
                }
            }
        }

        private static void ApplyLineClears(
            BoardState board,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns)
        {
            for (int i = 0; i < clearedRows.Count; i++)
            {
                int row = clearedRows[i];
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.GetType(x, row) >= 0)
                    {
                        MarkMatch(board, matchedFlags, x, row);
                    }
                }
            }

            for (int i = 0; i < clearedColumns.Count; i++)
            {
                int column = clearedColumns[i];
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.GetType(column, y) >= 0)
                    {
                        MarkMatch(board, matchedFlags, column, y);
                    }
                }
            }
        }

        private static void AddUnique(List<int> list, int value)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }

        private static void MarkMatch(BoardState board, bool[] matchedFlags, int x, int y)
        {
            matchedFlags[board.ToIndex(x, y)] = true;
        }

        private int CalculateSequenceScore(BoardSequence sequence, int comboIndex)
        {
            int score = sequence.MatchedPosition.Count * _config.ScorePerPiece;

            for (int i = 0; i < sequence.ClearedRows.Count; i++)
            {
                score += _config.LineClearBonus + _board.Width * _config.ScorePerCellInLineClear;
            }

            for (int i = 0; i < sequence.ClearedColumns.Count; i++)
            {
                score += _config.LineClearBonus + _board.Height * _config.ScorePerCellInLineClear;
            }

            float multiplier = 1f + comboIndex * _config.CascadeMultiplierStep;
            return Mathf.RoundToInt(score * multiplier);
        }

        private void EndGame(GameEndReason reason)
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            Events.RaiseGameEnded(new GameEndedEventArgs(reason, Score, TimeRemaining));
        }
    }
}
