using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public class BoardSequence
    {
        public List<MovedTileInfo> MovedTiles { get; set; }
        public List<AddedTileInfo> AddedTiles { get; set; }
        public List<Vector2Int> MatchedPosition { get; set; }
        public List<Vector2Int> MatchedBombs { get; set; }
        public List<int> ClearedRows { get; set; } = new();
        public List<int> ClearedColumns { get; set; } = new();
        public int ScoreDelta { get; set; }
        public int ComboIndex { get; set; }
        public float SkullTimePenalty { get; set; }
        public int SkullsCleared { get; set; }
    }
}
