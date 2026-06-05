using System;
using Gazeus.DesafioMatch3.Data;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public readonly struct TutorialGuideEventArgs
    {
        public bool IsActive { get; }
        public Vector2Int SelectCell { get; }
        public Vector2Int SwapTargetCell { get; }

        public TutorialGuideEventArgs(bool isActive, Vector2Int selectCell, Vector2Int swapTargetCell)
        {
            IsActive = isActive;
            SelectCell = selectCell;
            SwapTargetCell = swapTargetCell;
        }

        public static TutorialGuideEventArgs Inactive =>
            new(false, BoardCell.Invalid, BoardCell.Invalid);
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

    public readonly struct CountdownChangedEventArgs
    {
        public bool IsVisible { get; }
        public int StepNumber { get; }
        public bool IsGo { get; }

        private CountdownChangedEventArgs(bool isVisible, int stepNumber, bool isGo)
        {
            IsVisible = isVisible;
            StepNumber = stepNumber;
            IsGo = isGo;
        }

        public static CountdownChangedEventArgs ShowNumber(int step) => new(true, step, false);

        public static CountdownChangedEventArgs ShowGo() => new(true, 0, true);

        public static CountdownChangedEventArgs Hidden => new(false, 0, false);
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
