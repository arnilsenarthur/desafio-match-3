using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Localization;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public static class TutorialLayouts
    {
        public static TutorialStepDefinition[] BuildSteps(TileTypeRegistry registry, int boardWidth, int boardHeight)
        {
            int joker = registry.JokerTypeId;
            int bomb = registry.BombTypeId;
            int skull = registry.SkullTypeId;
            Vector2Int m = GetBoardMiddle(boardWidth, boardHeight);

            return new[]
            {
                Step(
                    LocKeys.TutorialStepMatch3Title,
                    LocKeys.TutorialStepMatch3Instruction,
                    m, selectDx: 0, selectDy: 0, swapDx: 1, swapDy: 0,
                    Tile(m, -1, 0, 0),
                    Tile(m, 0, 0, 1),
                    Tile(m, 1, 0, 0),
                    Tile(m, 2, 0, 0),
                    Tile(m, 3, 0, 1)),

                Step(
                    LocKeys.TutorialStepMatch4Title,
                    LocKeys.TutorialStepMatch4Instruction,
                    m, selectDx: 1, selectDy: 1, swapDx: 1, swapDy: 2,
                    Tile(m, 1, -2, 0),
                    Tile(m, 1, -1, 0),
                    Tile(m, 1, 0, 0),
                    Tile(m, 1, 1, 1),
                    Tile(m, 1, 2, 0)),

                Step(
                    LocKeys.TutorialStepJokerTitle,
                    LocKeys.TutorialStepJokerInstruction,
                    m, selectDx: 0, selectDy: 0, swapDx: 1, swapDy: 0,
                    Tile(m, -1, 0, 0),
                    Tile(m, 0, 0, 0),
                    Tile(m, 1, 0, joker),
                    Tile(m, 2, 0, 1),
                    Tile(m, 3, 0, 1)),

                Step(
                    LocKeys.TutorialStepBombTitle,
                    LocKeys.TutorialStepBombInstruction,
                    m, selectDx: 1, selectDy: 0, swapDx: 2, swapDy: 0,
                    Tile(m, -1, 0, 0),
                    Tile(m, 0, 0, 0),
                    Tile(m, 1, 0, 2),
                    Tile(m, 2, 0, bomb),
                    Tile(m, 3, 0, 2)),

                Step(
                    LocKeys.TutorialStepSkullTitle,
                    LocKeys.TutorialStepSkullInstruction,
                    m, selectDx: 0, selectDy: 0, swapDx: 1, swapDy: 0,
                    Tile(m, -1, 0, 0),
                    Tile(m, 0, 0, skull),
                    Tile(m, 1, 0, 1),
                    Tile(m, 2, 0, skull),
                    Tile(m, 3, 0, skull))
            };
        }

        private static Vector2Int GetBoardMiddle(int boardWidth, int boardHeight) =>
            BoardCell.At((boardWidth - 1) / 2, (boardHeight - 1) / 2);

        private static Vector2Int Offset(Vector2Int middle, int dx, int dy) =>
            BoardCell.At(middle.x + dx, middle.y + dy);

        private static (Vector2Int cell, int type) Tile(Vector2Int middle, int dx, int dy, int type) =>
            (Offset(middle, dx, dy), type);

        private static TutorialStepDefinition Step(
            string title,
            string instruction,
            Vector2Int middle,
            int selectDx,
            int selectDy,
            int swapDx,
            int swapDy,
            params (Vector2Int cell, int type)[] cells) =>
            new(
                title,
                instruction,
                Offset(middle, selectDx, selectDy),
                Offset(middle, swapDx, swapDy),
                cells);
    }
}
