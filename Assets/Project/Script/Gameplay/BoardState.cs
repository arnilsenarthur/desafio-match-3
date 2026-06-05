using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public class BoardState
    {
        private readonly Tile[] _cells;

        public BoardState(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Tile[width * height];

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new Tile(-1, -1);
            }
        }

        public int Width { get; }
        public int Height { get; }

        public int ToIndex(int x, int y) => y * Width + x;

        public int ToIndex(Vector2Int cell) => ToIndex(cell.x, cell.y);

        public int GetType(Vector2Int cell) => GetType(cell.x, cell.y);

        public int GetType(int x, int y) => _cells[ToIndex(x, y)].Type;

        public int GetId(Vector2Int cell) => GetId(cell.x, cell.y);

        public int GetId(int x, int y) => _cells[ToIndex(x, y)].Id;

        public void Set(Vector2Int cell, int id, int type) => Set(cell.x, cell.y, id, type);

        public void Set(int x, int y, int id, int type)
        {
            _cells[ToIndex(x, y)] = new Tile(id, type);
        }

        public void Clear(Vector2Int cell) => Clear(cell.x, cell.y);

        public void Clear(int x, int y) => Set(x, y, -1, -1);

        public void CopyFrom(BoardState other)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = other._cells[i];
            }
        }

        public void Swap(Vector2Int from, Vector2Int to) => Swap(from.x, from.y, to.x, to.y);

        public void Swap(int fromX, int fromY, int toX, int toY)
        {
            int fromIndex = ToIndex(fromX, fromY);
            int toIndex = ToIndex(toX, toY);
            (_cells[fromIndex], _cells[toIndex]) = (_cells[toIndex], _cells[fromIndex]);
        }
    }
}
