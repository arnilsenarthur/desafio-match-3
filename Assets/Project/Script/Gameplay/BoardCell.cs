using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public static class BoardCell
    {
        public static readonly Vector2Int Invalid = new(-1, -1);

        public static Vector2Int At(int x, int y) => new(x, y);

        public static bool IsValid(Vector2Int cell) => cell.x >= 0 && cell.y >= 0;

        public static bool AreAdjacent(Vector2Int a, Vector2Int b) =>
            IsValid(a) && IsValid(b) && Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

        public static bool MatchesSwap(Vector2Int from, Vector2Int to, Vector2Int expectedFrom, Vector2Int expectedTo) =>
            (from == expectedFrom && to == expectedTo) || (from == expectedTo && to == expectedFrom);
    }
}
