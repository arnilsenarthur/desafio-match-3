using System.Collections.Generic;
using Gazeus.DesafioMatch3.Data;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    internal sealed class GameState
    {
        private readonly GameConfig _config;
        private TileTypeRegistry _tileRegistry;

        private BoardState _board;
        private BoardState _workingBoard;
        private bool[] _matchedFlags;
        private List<int> _colorTypeIds;
        private List<int> _noMatchTypesScratch;
        private List<Vector2Int> _matchedPositions;
        private List<int> _clearedRowsScratch;
        private List<int> _clearedColumnsScratch;
        private List<MovedTileInfo> _movedTilesList;
        private List<AddedTileInfo> _addedTilesList;
        private readonly Dictionary<int, MovedTileInfo> _movedTilesById = new();
        private int _tileCount;
        private int _lastDisplayedTimeSeconds = -1;

        
        public bool IsGameOver { get; private set; }
        public bool TimerPaused { get; internal set; }
        public float TimeRemaining { get; private set; }
        public int Score { get; private set; }
        public BoardState Board => _board;
        public bool IsTutorialMode { get; private set; }

        public GameState(GameConfig config)
        {
            _config = config;
        }

        public bool TryStart(string difficultyId, out BoardState board)
        {
            board = null;

            if (!_config.TryGetDifficulty(difficultyId, out GameDifficultySettings difficulty))
            {
                Debug.LogError($"Unknown difficulty id: {difficultyId}");
                return false;
            }

            IsGameOver = false;
            Score = 0;
            _lastDisplayedTimeSeconds = -1;

            _tileRegistry = _config.TileTypeRegistry;
            if (_tileRegistry == null || !_tileRegistry.IsConfigured)
            {
                Debug.LogError(
                    "Assign a Tile Type Registry with color prefabs, joker, bomb, and skull prefabs.");
                return false;
            }

            int colorTypeCount = Mathf.Clamp(difficulty.TileTypeCount, 1, _tileRegistry.ColorCount);
            TimeRemaining = difficulty.StartingTimeSeconds;

            InitializeBoard(_config.BoardWidth, _config.BoardHeight, colorTypeCount);
            RaiseTimeIfDisplayChanged(force: true);

            GameService.NotifyGameStarted(new GameStartedEventArgs(
                difficulty.Id,
                TimeRemaining,
                _config.TargetScore));
            GameService.NotifyScoreChanged(new ScoreChangedEventArgs(Score, 0));

            board = _board;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (IsTutorialMode || IsGameOver || TimerPaused || deltaTime <= 0f)
            {
                return;
            }

            TimeRemaining = Mathf.Max(0f, TimeRemaining - deltaTime);
            RaiseTimeIfDisplayChanged(force: false);

            if (TimeRemaining <= 0f)
            {
                EndGame(GameEndReason.TimeUp);
            }
        }

        public void AdjustTime(float deltaSeconds)
        {
            if (IsGameOver || deltaSeconds == 0f)
            {
                return;
            }

            TimeRemaining = Mathf.Max(0f, TimeRemaining + deltaSeconds);
            RaiseTimeIfDisplayChanged(force: false);

            if (TimeRemaining <= 0f)
            {
                EndGame(GameEndReason.TimeUp);
            }
        }

        private void RaiseTimeIfDisplayChanged(bool force)
        {
            int displayedSeconds = Mathf.CeilToInt(TimeRemaining);
            if (!force && displayedSeconds == _lastDisplayedTimeSeconds)
            {
                return;
            }

            float delta = force ? 0f : displayedSeconds - _lastDisplayedTimeSeconds;
            _lastDisplayedTimeSeconds = displayedSeconds;
            GameService.NotifyTimeChanged(new TimeChangedEventArgs(TimeRemaining, delta));
        }

        public bool IsValidMovement(Vector2Int from, Vector2Int to)
        {
            if (IsGameOver)
            {
                return false;
            }

            return WouldCreateMatchAfterSwap(_board, from, to);
        }

        public bool HasValidMovement()
        {
            for (int y = 0; y < _board.Height; y++)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    if (x < _board.Width - 1 &&
                        WouldCreateMatchAfterSwap(_board, BoardCell.At(x, y), BoardCell.At(x + 1, y)))
                    {
                        return true;
                    }

                    if (y < _board.Height - 1 &&
                        WouldCreateMatchAfterSwap(_board, BoardCell.At(x, y), BoardCell.At(x, y + 1)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<BoardSequence> ResolveValidSwap(Vector2Int from, Vector2Int to)
        {
            if (!IsTutorialMode)
            {
                TimeRemaining += _config.TimeBonusPerValidSwap;
                RaiseTimeIfDisplayChanged(force: false);
            }

            List<BoardSequence> sequences = SwapTile(from, to);

            for (int i = 0; i < sequences.Count; i++)
            {
                BoardSequence sequence = sequences[i];
                sequence.ComboIndex = i;
                sequence.ScoreDelta = CalculateSequenceScore(sequence, i);

                Score += sequence.ScoreDelta;
                GameService.NotifyCascadeStep(new CascadeStepEventArgs(sequence, i, sequence.ScoreDelta));
                GameService.NotifyScoreChanged(new ScoreChangedEventArgs(Score, sequence.ScoreDelta));
            }

            if (!IsTutorialMode && _config.TargetScore > 0 && Score >= _config.TargetScore)
            {
                EndGame(GameEndReason.TargetScoreReached);
            }

            return sequences;
        }

        public void EnterTutorialMode()
        {
            IsTutorialMode = true;
        }

        public void ExitTutorialMode()
        {
            IsTutorialMode = false;
        }

        public bool IsTutorialSwapValid(Vector2Int from, Vector2Int to)
        {
            if (!IsTutorialMode || !_activeTutorialStep.MatchesExpectedSwap(from, to))
            {
                return false;
            }

            return WouldCreateMatchAfterSwap(_board, from, to);
        }

        public void RegenerateTutorialBaseBoard()
        {
            const int maxAttempts = 50;
            _tileCount = 0;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    for (int x = 0; x < _board.Width; x++)
                    {
                        Vector2Int cell = BoardCell.At(x, y);
                        int type = PickSafeColorType(_board, x, y);
                        _board.Set(cell, _tileCount++, type);
                    }
                }

                if (!HasImmediateMatches(_board))
                {
                    break;
                }
            }

            _workingBoard.CopyFrom(_board);
        }

        public void ApplyTutorialStep(TutorialStepDefinition step)
        {
            _activeTutorialStep = step;
            const int maxAttempts = 100;
            bool boardReady = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                BuildTutorialStepBoard(step);

                if (IsTutorialStepBoardValid(step))
                {
                    boardReady = true;
                    break;
                }
            }

            if (!boardReady)
            {
                Debug.LogWarning(
                    $"Tutorial step '{step.Title}' could not build a valid board after {maxAttempts} attempts.");
            }

            _workingBoard.CopyFrom(_board);
        }

        private bool IsTutorialStepBoardValid(TutorialStepDefinition step)
        {
            if (HasImmediateMatches(_board))
            {
                return false;
            }

            if (!BoardCell.AreAdjacent(step.SelectCell, step.SwapTargetCell))
            {
                return false;
            }

            return WouldCreateMatchAfterSwap(_board, step.SelectCell, step.SwapTargetCell);
        }

        private void BuildTutorialStepBoard(TutorialStepDefinition step)
        {
            HashSet<Vector2Int> scriptedCells = GetScriptedCells(step);
            _tileCount = 0;

            for (int y = 0; y < _board.Height; y++)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    _board.Clear(BoardCell.At(x, y));
                }
            }

            ApplyStepCells(step);

            for (int y = 0; y < _board.Height; y++)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    Vector2Int cell = BoardCell.At(x, y);
                    if (scriptedCells.Contains(cell))
                    {
                        continue;
                    }

                    int type = PickSafeColorType(_board, x, y);
                    _board.Set(cell, _tileCount++, type);
                }
            }
        }

        private static HashSet<Vector2Int> GetScriptedCells(TutorialStepDefinition step)
        {
            HashSet<Vector2Int> cells = new() { step.SelectCell, step.SwapTargetCell };
            (Vector2Int cell, int type)[] scripted = step.Cells;
            for (int i = 0; i < scripted.Length; i++)
            {
                cells.Add(scripted[i].cell);
            }

            return cells;
        }

        private TutorialStepDefinition _activeTutorialStep;

        private void ApplyStepCells(TutorialStepDefinition step)
        {
            (Vector2Int cell, int type)[] cells = step.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                (Vector2Int cell, int type) entry = cells[i];
                _board.Set(entry.cell, _tileCount++, entry.type);
            }
        }

        public bool TryRegenerateBoardIfNoValidMoves()
        {
            if (HasValidMovement())
            {
                return false;
            }

            RegenerateBoard();
            GameService.NotifyBoardRegenerated(new BoardRegeneratedEventArgs(_board));
            return true;
        }

        private bool WouldCreateMatchAfterSwap(BoardState board, Vector2Int from, Vector2Int to)
        {
            board.Swap(from, to);
            bool[] flags = new bool[board.Width * board.Height];
            List<int> rows = new();
            List<int> columns = new();
            bool valid = TileMatching.FindAndMarkMatches(board, _tileRegistry, flags, rows, columns);
            board.Swap(from, to);
            return valid;
        }

        private void InitializeBoard(int boardWidth, int boardHeight, int tileTypeCount)
        {
            _colorTypeIds = BuildColorTypeIds(tileTypeCount);
            EnsureBoards(boardWidth, boardHeight);
            RegenerateBoard();
        }

        private void RegenerateBoard()
        {
            const int maxAttempts = 50;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                CreateBoard(_board);

                if (HasValidMovement() && !HasImmediateMatches(_board))
                {
                    return;
                }
            }

            CreateBoard(_board);
        }

        private List<BoardSequence> SwapTile(Vector2Int from, Vector2Int to)
        {
            if (IsTutorialMode)
            {
                return SwapTileTutorial(from, to);
            }

            _workingBoard.CopyFrom(_board);
            _workingBoard.Swap(from, to);

            List<BoardSequence> boardSequences = new();
            bool hasMatches = FindMatches(_workingBoard, _matchedFlags, _clearedRowsScratch, _clearedColumnsScratch);

            while (hasMatches)
            {
                int skullsCleared = TileMatching.CountSkullsInMatches(_workingBoard, _tileRegistry, _matchedFlags);
                float skullPenalty = skullsCleared * _config.SkullTimePenaltySeconds;

                if (skullPenalty > 0f)
                {
                    AdjustTime(-skullPenalty);
                }

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

                        int spawnType = PickSpawnType(_workingBoard, x, y);
                        int id = _tileCount++;
                        _workingBoard.Set(x, y, id, spawnType);
                        _addedTilesList.Add(new AddedTileInfo(new Vector2Int(x, y), spawnType));
                    }
                }

                boardSequences.Add(new BoardSequence
                {
                    MatchedPosition = new List<Vector2Int>(_matchedPositions),
                    MovedTiles = new List<MovedTileInfo>(_movedTilesList),
                    AddedTiles = new List<AddedTileInfo>(_addedTilesList),
                    ClearedRows = new List<int>(_clearedRowsScratch),
                    ClearedColumns = new List<int>(_clearedColumnsScratch),
                    SkullsCleared = skullsCleared,
                    SkullTimePenalty = skullPenalty
                });

                hasMatches = FindMatches(_workingBoard, _matchedFlags, _clearedRowsScratch, _clearedColumnsScratch);
            }

            _board.CopyFrom(_workingBoard);
            return boardSequences;
        }

        private List<BoardSequence> SwapTileTutorial(Vector2Int from, Vector2Int to)
        {
            _workingBoard.CopyFrom(_board);
            _workingBoard.Swap(from, to);

            if (!FindMatches(_workingBoard, _matchedFlags, _clearedRowsScratch, _clearedColumnsScratch))
            {
                _board.CopyFrom(_workingBoard);
                return new List<BoardSequence>();
            }

            int skullsCleared = TileMatching.CountSkullsInMatches(_workingBoard, _tileRegistry, _matchedFlags);
            float skullPenalty = skullsCleared * _config.SkullTimePenaltySeconds;
            if (skullPenalty > 0f)
            {
                AdjustTime(-skullPenalty);
            }

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

            BoardSequence sequence = new()
            {
                MatchedPosition = new List<Vector2Int>(_matchedPositions),
                MovedTiles = new List<MovedTileInfo>(),
                AddedTiles = new List<AddedTileInfo>(),
                ClearedRows = new List<int>(_clearedRowsScratch),
                ClearedColumns = new List<int>(_clearedColumnsScratch),
                SkullsCleared = skullsCleared,
                SkullTimePenalty = skullPenalty
            };

            _board.CopyFrom(_workingBoard);
            return new List<BoardSequence> { sequence };
        }

        private int PickSpawnType(BoardState board, int x, int y)
        {
            if (IsTutorialMode)
            {
                return PickSafeColorType(board, x, y);
            }

            int specialType = RollSpecialTypeId();
            if (specialType >= 0 && !WouldCreateImmediateMatch(board, x, y, specialType))
            {
                return specialType;
            }

            return PickSafeColorType(board, x, y);
        }

        private int RollSpecialTypeId()
        {
            if (Random.value < _config.SkullSpawnChance)
            {
                return _tileRegistry.SkullTypeId;
            }

            if (Random.value < _config.BombJokerSpawnChance)
            {
                return _tileRegistry.BombTypeId;
            }

            if (Random.value < _config.JokerSpawnChance)
            {
                return _tileRegistry.JokerTypeId;
            }

            return -1;
        }

        private bool WouldCreateImmediateMatch(BoardState board, int x, int y, int type)
        {
            bool wasEmpty = board.GetType(x, y) < 0;
            int savedId = board.GetId(x, y);
            int savedType = board.GetType(x, y);

            board.Set(x, y, wasEmpty ? 0 : savedId, type);
            bool createsMatch = TileMatching.CreatesMatchAt(board, _tileRegistry, x, y);

            if (wasEmpty)
            {
                board.Clear(x, y);
            }
            else
            {
                board.Set(x, y, savedId, savedType);
            }

            return createsMatch;
        }

        private int PickSafeColorType(BoardState board, int x, int y)
        {
            _noMatchTypesScratch.Clear();
            for (int i = 0; i < _colorTypeIds.Count; i++)
            {
                _noMatchTypesScratch.Add(_colorTypeIds[i]);
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

            if (_noMatchTypesScratch.Count == 0)
            {
                return _colorTypeIds[Random.Range(0, _colorTypeIds.Count)];
            }

            for (int attempt = 0; attempt < _noMatchTypesScratch.Count; attempt++)
            {
                int candidate = _noMatchTypesScratch[Random.Range(0, _noMatchTypesScratch.Count)];
                if (!WouldCreateImmediateMatch(board, x, y, candidate))
                {
                    return candidate;
                }

                _noMatchTypesScratch.Remove(candidate);
            }

            return _colorTypeIds[Random.Range(0, _colorTypeIds.Count)];
        }

        private static List<int> BuildColorTypeIds(int colorTypeCount)
        {
            List<int> types = new(colorTypeCount);
            for (int i = 0; i < colorTypeCount; i++)
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
            _noMatchTypesScratch = new List<int>(_colorTypeIds?.Count ?? 4);
        }

        private bool HasImmediateMatches(BoardState board)
        {
            bool[] flags = new bool[board.Width * board.Height];
            List<int> rows = new();
            List<int> columns = new();
            return FindMatches(board, flags, rows, columns);
        }

        private bool FindMatches(
            BoardState board,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns)
        {
            if (!TileMatching.FindAndMarkMatches(board, _tileRegistry, matchedFlags, clearedRows, clearedColumns))
            {
                return false;
            }

            TileMatching.PropagateBombClears(
                board,
                _tileRegistry,
                matchedFlags,
                clearedRows,
                clearedColumns);
            return true;
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

        private void CreateBoard(BoardState board)
        {
            _tileCount = 0;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    int type = PickSpawnType(board, x, y);
                    board.Set(x, y, _tileCount++, type);
                }
            }
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
            GameService.NotifyGameEnded(new GameEndedEventArgs(reason, Score, TimeRemaining));
        }
    }
}

