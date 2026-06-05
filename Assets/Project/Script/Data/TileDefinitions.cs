using System;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Data
{
    [Serializable]
    public class TileDefinitions
    {
        [SerializeField]
        private string[] _colorTileIds =
        {
            "blue",
            "green",
            "orange",
            "yellow",
            "pink",
            "purple",
            "red",
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
            _colorTileIds is { Length: > 0 } &&
            !string.IsNullOrEmpty(_jokerId) &&
            !string.IsNullOrEmpty(_bombId) &&
            !string.IsNullOrEmpty(_skullId);

        public bool IsEmpty(string typeId) => string.IsNullOrEmpty(typeId);

        public bool IsColor(string typeId) => !IsEmpty(typeId) && Array.IndexOf(_colorTileIds, typeId) >= 0;

        public bool IsJoker(string typeId) => !IsEmpty(typeId) && typeId == _jokerId;

        public bool IsBomb(string typeId) => !IsEmpty(typeId) && typeId == _bombId;

        public bool IsSkull(string typeId) => !IsEmpty(typeId) && typeId == _skullId;
    }
}
