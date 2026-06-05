using System;
using System.Collections.Generic;
using Gazeus.DesafioMatch3.Gameplay;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Data
{
    [Serializable]
    public class TutorialTilePlacement
    {
        [SerializeField]
        private Vector2Int _offset;

        [SerializeField]
        private string _typeId;

        public Vector2Int Offset => _offset;
        public string TypeId => _typeId;

        public Vector2Int ResolveCell(int boardWidth, int boardHeight) =>
            TutorialStepDefinition.GetBoardCenter(boardWidth, boardHeight) + _offset;
    }

    [CreateAssetMenu(fileName = "TutorialStep", menuName = "Gameplay/Tutorial Step")]
    public class TutorialStepDefinition : ScriptableObject
    {
        [SerializeField]
        private string _titleKey;

        [SerializeField]
        private string _instructionKey;

        [SerializeField]
        private Sprite _illustrationSprite;

        [SerializeField]
        private Vector2Int _selectOffset;

        [SerializeField]
        private Vector2Int _swapOffset;

        [SerializeField]
        private TutorialTilePlacement[] _cells = Array.Empty<TutorialTilePlacement>();

        public string TitleKey => _titleKey;
        public string InstructionKey => _instructionKey;
        public Sprite IllustrationSprite => _illustrationSprite;
        public Vector2Int SelectOffset => _selectOffset;
        public Vector2Int SwapOffset => _swapOffset;

        public Vector2Int ResolveSelectCell(int boardWidth, int boardHeight) =>
            ResolveOffsetCell(boardWidth, boardHeight, _selectOffset);

        public Vector2Int ResolveSwapTargetCell(int boardWidth, int boardHeight) =>
            ResolveOffsetCell(boardWidth, boardHeight, _swapOffset);

        public bool MatchesExpectedSwap(Vector2Int from, Vector2Int to, int boardWidth, int boardHeight) =>
            BoardCell.MatchesSwap(
                from,
                to,
                ResolveSelectCell(boardWidth, boardHeight),
                ResolveSwapTargetCell(boardWidth, boardHeight));

        public void ResolveCells(
            int boardWidth,
            int boardHeight,
            ICollection<(Vector2Int cell, string typeId)> output)
        {
            if (output == null || _cells == null)
            {
                return;
            }

            for (int i = 0; i < _cells.Length; i++)
            {
                TutorialTilePlacement placement = _cells[i];
                if (placement == null || string.IsNullOrEmpty(placement.TypeId))
                {
                    continue;
                }

                output.Add((placement.ResolveCell(boardWidth, boardHeight), placement.TypeId));
            }
        }

        public static Vector2Int GetBoardCenter(int boardWidth, int boardHeight) =>
            BoardCell.At((boardWidth - 1) / 2, (boardHeight - 1) / 2);

        private static Vector2Int ResolveOffsetCell(int boardWidth, int boardHeight, Vector2Int offset) =>
            GetBoardCenter(boardWidth, boardHeight) + offset;
    }
}
