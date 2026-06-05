namespace Gazeus.DesafioMatch3.Gameplay
{
    public readonly struct Tile
    {
        public int Id { get; }
        public int Type { get; }

        public Tile(int id, int type)
        {
            Id = id;
            Type = type;
        }
    }
}
