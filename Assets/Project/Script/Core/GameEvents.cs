using System;
using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Core
{
    public sealed class GameEvents
    {
        public event Action<GameStartedEventArgs> GameStarted;
        public event Action<ScoreChangedEventArgs> ScoreChanged;
        public event Action<TimeChangedEventArgs> TimeChanged;
        public event Action<SwapStartedEventArgs> SwapStarted;
        public event Action<CascadeStepEventArgs> CascadeStep;
        public event Action<SwapCompletedEventArgs> SwapCompleted;
        public event Action<BoardRegeneratedEventArgs> BoardRegenerated;
        public event Action<GameEndedEventArgs> GameEnded;

        public void RaiseGameStarted(GameStartedEventArgs args) => GameStarted?.Invoke(args);
        public void RaiseScoreChanged(ScoreChangedEventArgs args) => ScoreChanged?.Invoke(args);
        public void RaiseTimeChanged(TimeChangedEventArgs args) => TimeChanged?.Invoke(args);
        public void RaiseSwapStarted(SwapStartedEventArgs args) => SwapStarted?.Invoke(args);
        public void RaiseCascadeStep(CascadeStepEventArgs args) => CascadeStep?.Invoke(args);
        public void RaiseSwapCompleted(SwapCompletedEventArgs args) => SwapCompleted?.Invoke(args);
        public void RaiseBoardRegenerated(BoardRegeneratedEventArgs args) => BoardRegenerated?.Invoke(args);
        public void RaiseGameEnded(GameEndedEventArgs args) => GameEnded?.Invoke(args);
    }

    public readonly struct GameStartedEventArgs
    {
        public string DifficultyId { get; }
        public float StartingTime { get; }
        public int TargetScore { get; }

        public GameStartedEventArgs(string difficultyId, float startingTime, int targetScore)
        {
            DifficultyId = difficultyId;
            StartingTime = startingTime;
            TargetScore = targetScore;
        }
    }

    public readonly struct ScoreChangedEventArgs
    {
        public int TotalScore { get; }
        public int Delta { get; }

        public ScoreChangedEventArgs(int totalScore, int delta)
        {
            TotalScore = totalScore;
            Delta = delta;
        }
    }

    public readonly struct TimeChangedEventArgs
    {
        public float TimeRemaining { get; }
        public float Delta { get; }

        public TimeChangedEventArgs(float timeRemaining, float delta)
        {
            TimeRemaining = timeRemaining;
            Delta = delta;
        }
    }

    public readonly struct SwapStartedEventArgs
    {
        public int FromX { get; }
        public int FromY { get; }
        public int ToX { get; }
        public int ToY { get; }

        public SwapStartedEventArgs(int fromX, int fromY, int toX, int toY)
        {
            FromX = fromX;
            FromY = fromY;
            ToX = toX;
            ToY = toY;
        }
    }

    public readonly struct CascadeStepEventArgs
    {
        public BoardSequence Sequence { get; }
        public int ComboIndex { get; }
        public int ScoreDelta { get; }

        public CascadeStepEventArgs(BoardSequence sequence, int comboIndex, int scoreDelta)
        {
            Sequence = sequence;
            ComboIndex = comboIndex;
            ScoreDelta = scoreDelta;
        }
    }

    public readonly struct SwapCompletedEventArgs
    {
        public IReadOnlyList<BoardSequence> Sequences { get; }
        public int TotalScoreDelta { get; }

        public SwapCompletedEventArgs(IReadOnlyList<BoardSequence> sequences, int totalScoreDelta)
        {
            Sequences = sequences;
            TotalScoreDelta = totalScoreDelta;
        }
    }

    public readonly struct BoardRegeneratedEventArgs
    {
        public BoardState Board { get; }

        public BoardRegeneratedEventArgs(BoardState board)
        {
            Board = board;
        }
    }

    public enum GameEndReason
    {
        TimeUp,
        TargetScoreReached
    }

    public readonly struct GameEndedEventArgs
    {
        public GameEndReason Reason { get; }
        public int FinalScore { get; }
        public float TimeRemaining { get; }

        public GameEndedEventArgs(GameEndReason reason, int finalScore, float timeRemaining)
        {
            Reason = reason;
            FinalScore = finalScore;
            TimeRemaining = timeRemaining;
        }
    }
}
