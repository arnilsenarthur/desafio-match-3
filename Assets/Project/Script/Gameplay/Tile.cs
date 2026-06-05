namespace Gazeus.DesafioMatch3.Gameplay
{
    public readonly struct Tile
    {
        public int Id { get; }
        public string TypeId { get; }

        public Tile(int id, string typeId)
        {
            Id = id;
            TypeId = typeId;
        }
    }
}
