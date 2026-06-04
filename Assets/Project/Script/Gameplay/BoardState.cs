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
                _cells[i] = new Tile { Id = -1, Type = -1 };
            }
        }

        public int Width { get; }
        public int Height { get; }

        public int ToIndex(int x, int y) => y * Width + x;

        public int GetType(int x, int y) => _cells[ToIndex(x, y)].Type;

        public int GetId(int x, int y) => _cells[ToIndex(x, y)].Id;

        public void Set(int x, int y, int id, int type)
        {
            int index = ToIndex(x, y);
            _cells[index].Id = id;
            _cells[index].Type = type;
        }

        public void Clear(int x, int y) => Set(x, y, -1, -1);

        public void CopyFrom(BoardState other)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = other._cells[i];
            }
        }

        public void Swap(int fromX, int fromY, int toX, int toY)
        {
            int fromIndex = ToIndex(fromX, fromY);
            int toIndex = ToIndex(toX, toY);
            (_cells[fromIndex], _cells[toIndex]) = (_cells[toIndex], _cells[fromIndex]);
        }
    }
}
