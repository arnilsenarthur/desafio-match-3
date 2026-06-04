using System;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Data
{
    [Serializable]
    public class GameDifficultySettings
    {
        [SerializeField]
        private string _id = "normal";

        [SerializeField]
        private string _displayName = "normal";

        [SerializeField]
        private float _startingTimeSeconds = 60f;

        [SerializeField]
        private int _tileTypeCount = 4;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float StartingTimeSeconds => _startingTimeSeconds;
        public int TileTypeCount => _tileTypeCount;
    }
}
