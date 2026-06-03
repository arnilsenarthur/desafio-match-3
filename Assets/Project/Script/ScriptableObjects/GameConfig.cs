using System;
using Gazeus.DesafioMatch3.Models;
using UnityEngine;

namespace Gazeus.DesafioMatch3.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Gameplay/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Board")]
        [SerializeField] private int _boardWidth = 10;
        [SerializeField] private int _boardHeight = 10;
        [SerializeField] private TilePrefabRepository _tilePrefabRepository;

        [Header("Difficulties")]
        [SerializeField] private GameDifficultySettings[] _difficulties =
        {
            new() { },
            new() { },
            new() { }
        };

        [Header("Timer")]
        [SerializeField] private float _timeBonusPerValidSwap = 2f;

        [Header("Score")]
        [SerializeField] private int _scorePerPiece = 10;
        [SerializeField] private int _lineClearBonus = 50;
        [SerializeField] private int _scorePerCellInLineClear = 5;
        [SerializeField] private float _cascadeMultiplierStep = 0.5f;
        [SerializeField] private int _targetScore;

        public int BoardWidth => _boardWidth;
        public int BoardHeight => _boardHeight;
        public TilePrefabRepository TilePrefabRepository => _tilePrefabRepository;
        public GameDifficultySettings[] Difficulties => _difficulties;
        public float TimeBonusPerValidSwap => _timeBonusPerValidSwap;
        public int ScorePerPiece => _scorePerPiece;
        public int LineClearBonus => _lineClearBonus;
        public int ScorePerCellInLineClear => _scorePerCellInLineClear;
        public float CascadeMultiplierStep => _cascadeMultiplierStep;
        public int TargetScore => _targetScore;

        public int MaxTileTypeCount =>
            _tilePrefabRepository != null ? _tilePrefabRepository.TileTypePrefabList.Length : 0;

        public bool TryGetDifficulty(string id, out GameDifficultySettings settings)
        {
            if (string.IsNullOrEmpty(id))
            {
                settings = null;
                return false;
            }

            foreach (GameDifficultySettings difficulty in _difficulties)
            {
                if (string.Equals(difficulty.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    settings = difficulty;
                    return true;
                }
            }

            settings = null;
            return false;
        }
    }
}
