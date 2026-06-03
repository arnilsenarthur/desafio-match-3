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

        [Header("Timer")]
        [SerializeField] private float _startingTimeSeconds = 60f;
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

        public int TileTypeCount =>
            _tilePrefabRepository != null ? _tilePrefabRepository.TileTypePrefabList.Length : 0;

        public float StartingTimeSeconds => _startingTimeSeconds;
        public float TimeBonusPerValidSwap => _timeBonusPerValidSwap;
        public int ScorePerPiece => _scorePerPiece;
        public int LineClearBonus => _lineClearBonus;
        public int ScorePerCellInLineClear => _scorePerCellInLineClear;
        public float CascadeMultiplierStep => _cascadeMultiplierStep;
        public int TargetScore => _targetScore;
    }
}
