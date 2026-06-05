using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gazeus.DesafioMatch3.Data
{
    [Serializable]
    public class GameDifficultySettings
    {
        [SerializeField]
        private string _id = "normal";

        [SerializeField]
        private float _startingTimeSeconds = 60f;

        [FormerlySerializedAs("_tileTypeCount")]
        [SerializeField]
        private string[] _tileIds = Array.Empty<string>();

        public string Id => _id;
        public float StartingTimeSeconds => _startingTimeSeconds;
        public string[] TileIds => _tileIds ?? Array.Empty<string>();
    }
}
