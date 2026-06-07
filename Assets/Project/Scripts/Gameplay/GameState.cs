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
        private bool[] _validationFlags;
        private List<int> _validationRows;
        private List<int> _validationColumns;
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

        public void ApplyTutorialStep(TutorialStepDefinition step)
        {
            _activeTutorialStep = step;
            const int maxAttempts = 100;
            bool boardReady = false;

            // use hard mode tiles
            if (!_config.TryGetDifficulty("hard", out GameDifficultySettings difficulty))
            {
                Debug.LogWarning($"Not able to get hard mode difficulty");
            }

            _shapeTypeIds = BuildShapeTypeIds(difficulty.TileIds, _tiles);
            if (_shapeTypeIds.Count == 0)
            {
                Debug.LogWarning($"Difficulty '{difficulty.Id}' has no valid shape tile ids.");
            }

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
            // Filler tiles must avoid the shapes the step itself places (e.g. the match-3 step
            // uses circle + square, so no other cell may be a circle or square), keeping the
            // scripted match the only one on the board.
            HashSet<string> reservedShapes = GetStepShapeTypes(step);
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

                    string typeId = PickSafeShapeType(_board, x, y, reservedShapes);
                    _board.Set(cell, _tileCount++, typeId);
                }
            }
        }

        private HashSet<string> GetStepShapeTypes(TutorialStepDefinition step)
        {
            HashSet<string> shapes = new();

            var resolvedCells = new List<(Vector2Int cell, string typeId)>();
            step.ResolveCells(_board.Width, _board.Height, resolvedCells);

            for (int i = 0; i < resolvedCells.Count; i++)
            {
                string typeId = resolvedCells[i].typeId;
                if (_tiles.IsShape(typeId))
                {
                    shapes.Add(typeId);
                }
            }

            return shapes;
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
            System.Array.Clear(_validationFlags, 0, _validationFlags.Length);
            _validationRows.Clear();
            _validationColumns.Clear();
            bool valid = TileMatching.FindAndMarkMatches(
                board, _tiles, _validationFlags, _validationRows, _validationColumns);
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

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                CreateBoard(_board);

                if (HasValidMovement())
                {
                    return;
                }
            }

            Debug.LogWarning(
                "RegenerateBoard could not produce a board with valid moves; using last attempt.");
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

            // Resolve the whole cascade up front on the working board: each iteration clears
            // the current matches, drops tiles by gravity, refills empties, and records one
            // BoardSequence (one animation step) before re-scanning for chained matches.
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

                // Refill the empty cells left at the top of each column after gravity.
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
                    MatchedPositions = new List<Vector2Int>(_matchedPositions),
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
                MatchedPositions = new List<Vector2Int>(_matchedPositions),
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

            if (Random.value < _config.BombSpawnChance)
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

        private string PickSafeShapeType(BoardState board, int x, int y) =>
            PickSafeShapeType(board, x, y, null);

        // excludedShapes (optional) are shape ids the caller forbids, e.g. the tutorial step's
        // own shapes. They are filtered out of the candidate pool and only fall back to as a
        // last resort if no other shape is configured.
        private string PickSafeShapeType(BoardState board, int x, int y, HashSet<string> excludedShapes)
        {
            _noMatchTypesScratch.Clear();
            for (int i = 0; i < _shapeTypeIds.Count; i++)
            {
                string shapeId = _shapeTypeIds[i];
                if (excludedShapes != null && excludedShapes.Contains(shapeId))
                {
                    continue;
                }

                _noMatchTypesScratch.Add(shapeId);
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
                return PickRandomShape(excludedShapes);
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

            return PickRandomShape(excludedShapes);
        }

        private string PickRandomShape(HashSet<string> excludedShapes)
        {
            if (excludedShapes != null && excludedShapes.Count > 0)
            {
                _noMatchTypesScratch.Clear();
                for (int i = 0; i < _shapeTypeIds.Count; i++)
                {
                    if (!excludedShapes.Contains(_shapeTypeIds[i]))
                    {
                        _noMatchTypesScratch.Add(_shapeTypeIds[i]);
                    }
                }

                if (_noMatchTypesScratch.Count > 0)
                {
                    return _noMatchTypesScratch[Random.Range(0, _noMatchTypesScratch.Count)];
                }
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
            _validationFlags = new bool[width * height];
            _validationRows = new List<int>();
            _validationColumns = new List<int>();
        }

        private bool HasImmediateMatches(BoardState board)
        {
            System.Array.Clear(_validationFlags, 0, _validationFlags.Length);
            _validationRows.Clear();
            _validationColumns.Clear();
            return TileMatching.FindAndMarkMatches(
                board, _tiles, _validationFlags, _validationRows, _validationColumns);
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

            // Snapshot the matches before bombs detonate. A bomb only counts as "triggered"
            // (for VFX/scoring) if it was matched directly, not if it was merely caught in
            // another bomb's row/column blast, so we compare against this pre-blast state.
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

                    // Only bombs that were part of the original match (pre-blast) drive the
                    // explosion effect; bombs cleared by another bomb's blast are skipped.
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
            int score = sequence.MatchedPositions.Count * _config.ScorePerPiece;

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

