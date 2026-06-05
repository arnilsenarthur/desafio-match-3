using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public readonly struct AddedTileInfo
    {
        public Vector2Int Position { get; }
        public int Type { get; }

        public AddedTileInfo(Vector2Int position, int type)
        {
            Position = position;
            Type = type;
        }
    }
}
