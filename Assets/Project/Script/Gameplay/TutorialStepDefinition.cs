using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    /// <summary>Title and Instruction are localization keys for <see cref="Localization.LocalizationService.Localize"/>.</summary>
    public readonly struct TutorialStepDefinition
    {
        public string Title { get; }
        public string Instruction { get; }
        public Vector2Int SelectCell { get; }
        public Vector2Int SwapTargetCell { get; }
        public (Vector2Int cell, int type)[] Cells { get; }

        public TutorialStepDefinition(
            string title,
            string instruction,
            Vector2Int selectCell,
            Vector2Int swapTargetCell,
            (Vector2Int cell, int type)[] cells)
        {
            Title = title;
            Instruction = instruction;
            SelectCell = selectCell;
            SwapTargetCell = swapTargetCell;
            Cells = cells;
        }

        public bool MatchesExpectedSwap(Vector2Int from, Vector2Int to) =>
            BoardCell.MatchesSwap(from, to, SelectCell, SwapTargetCell);
    }
}
