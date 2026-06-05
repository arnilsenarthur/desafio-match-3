using System;
using System.Collections.Generic;
using Gazeus.DesafioMatch3.Data;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    /// <summary>Static gameplay facade: session events and UI-facing entry points. Match logic lives in <see cref="GameState"/>.</summary>
    public static class GameService
    {
        private static GameState _state;
        private static GameController _controller;

        public static event Action<GameStartedEventArgs> GameStarted;
        public static event Action<ScoreChangedEventArgs> ScoreChanged;
        public static event Action<TimeChangedEventArgs> TimeChanged;
        public static event Action<CascadeStepEventArgs> CascadeStep;
        public static event Action<BoardRegeneratedEventArgs> BoardRegenerated;
        public static event Action<GameEndedEventArgs> GameEnded;
        public static event Action<CountdownChangedEventArgs> CountdownChanged;
        public static event Action<TutorialGuideEventArgs> TutorialGuideChanged;

        public static bool IsActive => _state != null;
        public static bool IsGameOver => _state != null && _state.IsGameOver;
        public static bool IsTutorialMode => _state != null && _state.IsTutorialMode;
        public static bool IsPaused => _controller != null && _controller.IsPaused;
        public static float TimeRemaining => _state?.TimeRemaining ?? 0f;
        public static int Score => _state?.Score ?? 0;
        public static BoardState Board => _state?.Board;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            EndSession();
            DetachAll();
            GameStarted = null;
            ScoreChanged = null;
            TimeChanged = null;
            CascadeStep = null;
            BoardRegenerated = null;
            GameEnded = null;
            CountdownChanged = null;
            TutorialGuideChanged = null;
        }

        public static void Attach(GameController controller) => _controller = controller;

        public static void Detach(GameController controller)
        {
            if (_controller == controller)
            {
                _controller = null;
            }
        }

        private static void DetachAll() => _controller = null;

        public static void BeginSession(GameConfig config)
        {
            EndSession();
            if (config != null)
            {
                _state = new GameState(config);
            }
        }

        public static void EndSession() => _state = null;

        public static void RestartCurrentGame() => _controller?.RestartCurrentGame();

        public static void RestartGame(string difficultyId) => _controller?.RestartGame(difficultyId);

        public static void SetPaused(bool paused) => _controller?.SetPaused(paused);

        public static void LockForGameOver() => _controller?.LockForGameOver();

        public static bool TryStart(string difficultyId, out BoardState board)
        {
            if (_state == null)
            {
                board = null;
                return false;
            }

            return _state.TryStart(difficultyId, out board);
        }

        public static void Tick(float deltaTime) => _state?.Tick(deltaTime);

        public static void AdjustTime(float deltaSeconds) => _state?.AdjustTime(deltaSeconds);

        public static bool IsValidMovement(Vector2Int from, Vector2Int to) =>
            _state != null && _state.IsValidMovement(from, to);

        public static bool IsTutorialSwapValid(Vector2Int from, Vector2Int to) =>
            _state != null && _state.IsTutorialSwapValid(from, to);

        public static List<BoardSequence> ResolveValidSwap(Vector2Int from, Vector2Int to) =>
            _state?.ResolveValidSwap(from, to);

        public static bool TryRegenerateBoardIfNoValidMoves() =>
            _state != null && _state.TryRegenerateBoardIfNoValidMoves();

        public static void EnterTutorialMode() => _state?.EnterTutorialMode();

        public static void ExitTutorialMode() => _state?.ExitTutorialMode();

        public static void RegenerateTutorialBaseBoard() => _state?.RegenerateTutorialBaseBoard();

        public static void ApplyTutorialStep(TutorialStepDefinition step) => _state?.ApplyTutorialStep(step);

        public static bool TimerPaused
        {
            get => _state != null && _state.TimerPaused;
            set
            {
                if (_state != null)
                {
                    _state.TimerPaused = value;
                }
            }
        }

        internal static void NotifyGameStarted(GameStartedEventArgs args) => GameStarted?.Invoke(args);

        internal static void NotifyScoreChanged(ScoreChangedEventArgs args) => ScoreChanged?.Invoke(args);

        internal static void NotifyTimeChanged(TimeChangedEventArgs args) => TimeChanged?.Invoke(args);

        internal static void NotifyCascadeStep(CascadeStepEventArgs args) => CascadeStep?.Invoke(args);

        internal static void NotifyBoardRegenerated(BoardRegeneratedEventArgs args) =>
            BoardRegenerated?.Invoke(args);

        internal static void NotifyGameEnded(GameEndedEventArgs args) => GameEnded?.Invoke(args);

        internal static void NotifyCountdownChanged(CountdownChangedEventArgs args) =>
            CountdownChanged?.Invoke(args);

        internal static void NotifyTutorialGuideChanged(TutorialGuideEventArgs args) =>
            TutorialGuideChanged?.Invoke(args);
    }
}
