using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Models
{
    public class BoardSequence
    {
        public List<MovedTileInfo> MovedTiles { get; set; }
        public List<AddedTileInfo> AddedTiles { get; set; }
        public List<Vector2Int> MatchedPosition { get; set; }
        public List<int> ClearedRows { get; set; } = new();
        public List<int> ClearedColumns { get; set; } = new();
        public int ScoreDelta { get; set; }
        public int ComboIndex { get; set; }
    }
}
