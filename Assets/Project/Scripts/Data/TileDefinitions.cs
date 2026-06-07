using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gazeus.DesafioMatch3.Data
{
    [Serializable]
    public class TileDefinitions
    {
        [SerializeField]
        private string[] _shapeTileIds =
        {
            "circle",
            "square",
            "pentagon",
            "hexagon",
            "triangle",
            "diamond",
            "star",
        };

        [SerializeField]
        private string _jokerId = "joker";

        [SerializeField]
        private string _bombId = "bomb";

        [SerializeField]
        private string _skullId = "skull";

        public string JokerId => _jokerId;
        public string BombId => _bombId;
        public string SkullId => _skullId;

        public bool IsConfigured =>
            _shapeTileIds is { Length: > 0 } &&
            !string.IsNullOrEmpty(_jokerId) &&
            !string.IsNullOrEmpty(_bombId) &&
            !string.IsNullOrEmpty(_skullId);

        public bool IsEmpty(string typeId) => string.IsNullOrEmpty(typeId);

        public bool IsShape(string typeId) => !IsEmpty(typeId) && Array.IndexOf(_shapeTileIds, typeId) >= 0;

        public bool IsJoker(string typeId) => !IsEmpty(typeId) && typeId == _jokerId;

        public bool IsBomb(string typeId) => !IsEmpty(typeId) && typeId == _bombId;

        public bool IsSkull(string typeId) => !IsEmpty(typeId) && typeId == _skullId;
    }
}
