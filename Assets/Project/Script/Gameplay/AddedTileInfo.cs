using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public readonly struct AddedTileInfo
    {
        public Vector2Int Position { get; }
        public string TypeId { get; }

        public AddedTileInfo(Vector2Int position, string typeId)
        {
            Position = position;
            TypeId = typeId;
        }
    }
}
