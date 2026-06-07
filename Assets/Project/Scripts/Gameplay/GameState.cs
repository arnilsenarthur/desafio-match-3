using System.Collections.Generic;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Data;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    internal sealed class GameState
    {
        private readonly GameConfig _config;
        private TileDefinitions _tiles;

        private BoardState _board;
        private BoardState _workingBoard;
        private bool[] _matchedFlags;
        private bool[] _preBombMatchedFlags;
        private List<string> _shapeTypeIds;
        private List<string> _noMatchTypesScratch;
        private List<Vector2Int> _matchedPositions;
        private List<Vector2Int> _matchedBombsScratch;
        private List<int> _clearedRowsScratch;
        private List<int> _clearedColumnsScratch;
        private List<MovedTileInfo> _movedTilesList;
        private List<AddedTileInfo> _addedTilesList;
        private readonly List<BoardState> _cascadeBoardSnapshots = new();
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
            IsTutorialMode = false;
            TimerPaused = false;
            Score = 0;
            _lastDisplayedTimeSeconds = -1;
            _activeTutorialStep = null;
            _cascadeBoardSnapshots.Clear();

            _tiles = _config.Tiles;
            if (_tiles == null || !_tiles.IsConfigured)
            {
                Debug.LogError(
                    "Assign tile definitions with shape prefabs, joker, bomb, and skull prefabs.");
                return false;
            }

            _shapeTypeIds = BuildShapeTypeIds(difficulty.TileIds, _tiles);
            if (_shapeTypeIds.Count == 0)
            {
                Debug.LogError($"Difficulty '{difficulty.Id}' has no valid shape tile ids.");
                return false;
            }

            TimeRemaining = difficulty.StartingTimeSeconds;

            InitializeBoard(_config.BoardWidth, _config.BoardHeight);
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
            if (IsTutorialMode || IsGameOver || TimerPaused || GameService.IsPaused || deltaTime <= 0f)
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
            if (IsGameOver || deltaSeconds == 0f || TimerPaused || GameService.IsPaused)
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
            }

            return sequences;
        }

        public void ApplyCascadeStep(BoardSequence sequence)
        {
            if (sequence == null)
            {
                return;
            }

            Score += sequence.ScoreDelta;
            ApplyBoardSnapshot(sequence.ComboIndex);
            GameService.NotifyCascadeStep(
                new CascadeStepEventArgs(sequence, sequence.ComboIndex, sequence.ScoreDelta));
            GameService.NotifyScoreChanged(new ScoreChangedEventArgs(Score, sequence.ScoreDelta));

            if (!IsTutorialMode && _config.TargetScore > 0 && Score >= _config.TargetScore)
            {
                EndGame(GameEndReason.TargetScoreReached);
            }
        }

        public void EnterTutorialMode()
        {
            IsTutorialMode = true;
        }

        public void ExitTutorialMode()
        {
            IsTutorialMode = false;
            _activeTutorialStep = null;
        }

        public bool IsTutorialSwapValid(Vector2Int from, Vector2Int to)
        {
            if (!IsTutorialMode ||
                !_activeTutorialStep.MatchesExpectedSwap(from, to, _board.Width, _board.Height))
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
                        string typeId = PickSafeShapeType(_board, x, y);
                        _board.Set(cell, _tileCount++, typeId);
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
                    $"Tutorial step '{step.name}' could not build a valid board after {maxAttempts} attempts.");
            }

            _workingBoard.CopyFrom(_board);
        }

        private bool IsTutorialStepBoardValid(TutorialStepDefinition step)
        {
            if (HasImmediateMatches(_board))
            {
                return false;
            }

            Vector2Int selectCell = step.ResolveSelectCell(_board.Width, _board.Height);
            Vector2Int swapTargetCell = step.ResolveSwapTargetCell(_board.Width, _board.Height);

            if (!BoardCell.AreAdjacent(selectCell, swapTargetCell))
            {
                return false;
            }

            return WouldCreateMatchAfterSwap(_board, selectCell, swapTargetCell);
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

                    string typeId = PickSafeShapeType(_board, x, y);
                    _board.Set(cell, _tileCount++, typeId);
                }
            }
        }

        private HashSet<Vector2Int> GetScriptedCells(TutorialStepDefinition step)
        {
            HashSet<Vector2Int> cells = new()
            {
                step.ResolveSelectCell(_board.Width, _board.Height),
                step.ResolveSwapTargetCell(_board.Width, _board.Height),
            };

            var resolvedCells = new List<(Vector2Int cell, string typeId)>();
            step.ResolveCells(_board.Width, _board.Height, resolvedCells);

            for (int i = 0; i < resolvedCells.Count; i++)
            {
                cells.Add(resolvedCells[i].cell);
            }

            return cells;
        }

        private TutorialStepDefinition _activeTutorialStep;

        private void ApplyStepCells(TutorialStepDefinition step)
        {
            var cells = new List<(Vector2Int cell, string typeId)>();
            step.ResolveCells(_board.Width, _board.Height, cells);

            for (int i = 0; i < cells.Count; i++)
            {
                (Vector2Int cell, string typeId) entry = cells[i];
                _board.Set(entry.cell, _tileCount++, entry.typeId);
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
            bool valid = TileMatching.FindAndMarkMatches(board, _tiles, flags, rows, columns);
            board.Swap(from, to);
            return valid;
        }

        private void InitializeBoard(int boardWidth, int boardHeight)
        {
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
            _board.Swap(from, to);

            List<BoardSequence> boardSequences = new();
            _cascadeBoardSnapshots.Clear();
            bool hasMatches = FindMatches(_workingBoard, _matchedFlags, _clearedRowsScratch, _clearedColumnsScratch);

            while (hasMatches)
            {
                int skullsCleared = TileMatching.CountSkullsInMatches(_workingBoard, _tiles, _matchedFlags);
                float skullPenalty = skullsCleared * _config.SkullTimePenaltySeconds;

                if (skullPenalty > 0f)
                {
                    AdjustTime(-skullPenalty);
                }

                CollectMatchedPositions(_workingBoard, _matchedFlags);
                ClearMatchedCells(_workingBoard, _matchedPositions);

                ApplyColumnGravity(_workingBoard);

                _addedTilesList.Clear();
                for (int y = _workingBoard.Height - 1; y > -1; y--)
                {
                    for (int x = _workingBoard.Width - 1; x > -1; x--)
                    {
                        if (!_tiles.IsEmpty(_workingBoard.GetType(x, y)))
                        {
                            continue;
                        }

                        string spawnTypeId = PickSpawnType(_workingBoard, x, y);
                        int id = _tileCount++;
                        _workingBoard.Set(x, y, id, spawnTypeId);
                        _addedTilesList.Add(new AddedTileInfo(new Vector2Int(x, y), spawnTypeId));
                    }
                }

                boardSequences.Add(new BoardSequence
                {
                    MatchedPosition = new List<Vector2Int>(_matchedPositions),
                    MatchedBombs = new List<Vector2Int>(_matchedBombsScratch),
                    MovedTiles = new List<MovedTileInfo>(_movedTilesList),
                    AddedTiles = new List<AddedTileInfo>(_addedTilesList),
                    ClearedRows = new List<int>(_clearedRowsScratch),
                    ClearedColumns = new List<int>(_clearedColumnsScratch),
                    SkullsCleared = skullsCleared,
                    SkullTimePenalty = skullPenalty
                });

                CaptureCascadeSnapshot();
                hasMatches = FindMatches(_workingBoard, _matchedFlags, _clearedRowsScratch, _clearedColumnsScratch);
            }

            return boardSequences;
        }

        private List<BoardSequence> SwapTileTutorial(Vector2Int from, Vector2Int to)
        {
            _workingBoard.CopyFrom(_board);
            _workingBoard.Swap(from, to);
            _board.Swap(from, to);
            _cascadeBoardSnapshots.Clear();

            if (!FindMatches(_workingBoard, _matchedFlags, _clearedRowsScratch, _clearedColumnsScratch))
            {
                _board.CopyFrom(_workingBoard);
                return new List<BoardSequence>();
            }

            int skullsCleared = TileMatching.CountSkullsInMatches(_workingBoard, _tiles, _matchedFlags);
            float skullPenalty = skullsCleared * _config.SkullTimePenaltySeconds;
            if (skullPenalty > 0f)
            {
                AdjustTime(-skullPenalty);
            }

            CollectMatchedPositions(_workingBoard, _matchedFlags);
            ClearMatchedCells(_workingBoard, _matchedPositions);

            BoardSequence sequence = new()
            {
                MatchedPosition = new List<Vector2Int>(_matchedPositions),
                MatchedBombs = new List<Vector2Int>(_matchedBombsScratch),
                MovedTiles = new List<MovedTileInfo>(),
                AddedTiles = new List<AddedTileInfo>(),
                ClearedRows = new List<int>(_clearedRowsScratch),
                ClearedColumns = new List<int>(_clearedColumnsScratch),
                SkullsCleared = skullsCleared,
                SkullTimePenalty = skullPenalty
            };

            CaptureCascadeSnapshot();
            return new List<BoardSequence> { sequence };
        }

        private void CaptureCascadeSnapshot()
        {
            var snapshot = new BoardState(_workingBoard.Width, _workingBoard.Height);
            snapshot.CopyFrom(_workingBoard);
            _cascadeBoardSnapshots.Add(snapshot);
        }

        private void ApplyBoardSnapshot(int comboIndex)
        {
            if (comboIndex < 0 || comboIndex >= _cascadeBoardSnapshots.Count)
            {
                return;
            }

            _board.CopyFrom(_cascadeBoardSnapshots[comboIndex]);
        }

        private string PickSpawnType(BoardState board, int x, int y)
        {
            if (IsTutorialMode)
            {
                return PickSafeShapeType(board, x, y);
            }

            string specialTypeId = RollSpecialTypeId();
            if (!string.IsNullOrEmpty(specialTypeId) && !WouldCreateImmediateMatch(board, x, y, specialTypeId))
            {
                return specialTypeId;
            }

            return PickSafeShapeType(board, x, y);
        }

        private string RollSpecialTypeId()
        {
            if (Random.value < _config.SkullSpawnChance)
            {
                return _tiles.SkullId;
            }

            if (Random.value < _config.BombJokerSpawnChance)
            {
                return _tiles.BombId;
            }

            if (Random.value < _config.JokerSpawnChance)
            {
                return _tiles.JokerId;
            }

            return null;
        }

        private bool WouldCreateImmediateMatch(BoardState board, int x, int y, string typeId)
        {
            bool wasEmpty = _tiles.IsEmpty(board.GetType(x, y));
            int savedId = board.GetId(x, y);
            string savedTypeId = board.GetType(x, y);

            board.Set(x, y, wasEmpty ? 0 : savedId, typeId);
            bool createsMatch = TileMatching.CreatesMatchAt(board, _tiles, x, y);

            if (wasEmpty)
            {
                board.Clear(x, y);
            }
            else
            {
                board.Set(x, y, savedId, savedTypeId);
            }

            return createsMatch;
        }

        private string PickSafeShapeType(BoardState board, int x, int y)
        {
            _noMatchTypesScratch.Clear();
            for (int i = 0; i < _shapeTypeIds.Count; i++)
            {
                _noMatchTypesScratch.Add(_shapeTypeIds[i]);
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
                return _shapeTypeIds[Random.Range(0, _shapeTypeIds.Count)];
            }

            for (int attempt = 0; attempt < _noMatchTypesScratch.Count; attempt++)
            {
                string candidate = _noMatchTypesScratch[Random.Range(0, _noMatchTypesScratch.Count)];
                if (!WouldCreateImmediateMatch(board, x, y, candidate))
                {
                    return candidate;
                }

                _noMatchTypesScratch.Remove(candidate);
            }

            return _shapeTypeIds[Random.Range(0, _shapeTypeIds.Count)];
        }

        private static List<string> BuildShapeTypeIds(string[] tileIds, TileDefinitions tiles)
        {
            List<string> types = new(tileIds?.Length ?? 0);

            if (tileIds == null)
            {
                return types;
            }

            for (int i = 0; i < tileIds.Length; i++)
            {
                string tileId = tileIds[i];
                if (!string.IsNullOrEmpty(tileId) && tiles.IsShape(tileId) && !types.Contains(tileId))
                {
                    types.Add(tileId);
                }
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
            _preBombMatchedFlags = new bool[width * height];
            _matchedPositions = new List<Vector2Int>(width * height);
            _matchedBombsScratch = new List<Vector2Int>(8);
            _clearedRowsScratch = new List<int>();
            _clearedColumnsScratch = new List<int>();
            _movedTilesList = new List<MovedTileInfo>(width * height);
            _addedTilesList = new List<AddedTileInfo>(width * height);
            _noMatchTypesScratch = new List<string>(_shapeTypeIds?.Count ?? 4);
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
            if (!TileMatching.FindAndMarkMatches(board, _tiles, matchedFlags, clearedRows, clearedColumns))
            {
                return false;
            }

            for (int i = 0; i < matchedFlags.Length; i++)
            {
                _preBombMatchedFlags[i] = matchedFlags[i];
            }

            TileMatching.PropagateBombClears(board, _tiles, matchedFlags, clearedRows, clearedColumns);
            return true;
        }

        private void CollectMatchedPositions(BoardState board, bool[] matchedFlags)
        {
            _matchedPositions.Clear();
            _matchedBombsScratch.Clear();

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    int index = board.ToIndex(x, y);
                    if (!matchedFlags[index])
                    {
                        continue;
                    }

                    Vector2Int cell = new(x, y);
                    _matchedPositions.Add(cell);

                    if (_preBombMatchedFlags[index] && _tiles.IsBomb(board.GetType(x, y)))
                    {
                        _matchedBombsScratch.Add(cell);
                    }
                }
            }
        }

        private static void ClearMatchedCells(BoardState board, List<Vector2Int> matchedCells)
        {
            for (int i = 0; i < matchedCells.Count; i++)
            {
                Vector2Int cell = matchedCells[i];
                board.Clear(cell.x, cell.y);
            }
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
                    string typeId = board.GetType(x, readY);
                    if (_tiles.IsEmpty(typeId))
                    {
                        continue;
                    }

                    int id = board.GetId(x, readY);

                    if (readY != writeY)
                    {
                        board.Set(x, writeY, id, typeId);
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
                    string typeId = PickSpawnType(board, x, y);
                    board.Set(x, y, _tileCount++, typeId);
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
            bool isNewHighScore = HighScoreStorage.TrySetHighScore(
                GameRunContext.SelectedDifficultyId,
                Score);
            GameService.NotifyGameEnded(new GameEndedEventArgs(reason, Score, TimeRemaining, isNewHighScore));
        }
    }
}

