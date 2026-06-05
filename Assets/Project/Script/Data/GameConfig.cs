using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gazeus.DesafioMatch3.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Gameplay/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Board")]
        [SerializeField]
        private int _boardWidth = 10;

        [SerializeField]
        private int _boardHeight = 10;

        [Header("Tiles")]
        [FormerlySerializedAs("_tileTypeRegistry")]
        [SerializeField]
        private TileDefinitions _tiles = new();

        [Header("Difficulties")]
        [SerializeField]
        private GameDifficultySettings[] _difficulties =
        {
            new() { },
            new() { },
            new() { }
        };

        [Header("Timer")]
        [SerializeField]
        private float _timeBonusPerValidSwap = 2f;

        [Header("Countdown")]
        [SerializeField]
        private float _delayBeforeCountdown = 0f;

        [SerializeField]
        private int _countdownLength = 3;

        [SerializeField]
        private float _countdownStepDuration = 1f;

        [SerializeField]
        private float _goDisplayDuration = 0.5f;

        [Header("Score")]
        [SerializeField]
        private int _scorePerPiece = 10;

        [SerializeField]
        private int _lineClearBonus = 50;

        [SerializeField]
        private int _scorePerCellInLineClear = 5;

        [SerializeField]
        private float _cascadeMultiplierStep = 0.5f;

        [SerializeField]
        private int _targetScore;

        [Header("Special Tiles (spawn chance 0–1)")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _jokerSpawnChance = 0.03f;

        [Range(0f, 1f)]
        [SerializeField]
        private float _bombJokerSpawnChance = 0.02f;

        [Range(0f, 1f)]
        [SerializeField]
        private float _skullSpawnChance = 0.02f;

        [SerializeField]
        private float _skullTimePenaltySeconds = 5f;

        [Header("Tutorial")]
        [SerializeField]
        private TutorialStepDefinition[] _tutorialSteps = Array.Empty<TutorialStepDefinition>();

        public int BoardWidth => _boardWidth;
        public int BoardHeight => _boardHeight;
        public TileDefinitions Tiles => _tiles;
        public GameDifficultySettings[] Difficulties => _difficulties;
        public float TimeBonusPerValidSwap => _timeBonusPerValidSwap;
        public float DelayBeforeCountdown => Mathf.Max(0f, _delayBeforeCountdown);
        public int CountdownLength => Mathf.Max(0, _countdownLength);
        public float CountdownStepDuration => Mathf.Max(0.01f, _countdownStepDuration);
        public float GoDisplayDuration => Mathf.Max(0f, _goDisplayDuration);
        public int ScorePerPiece => _scorePerPiece;
        public int LineClearBonus => _lineClearBonus;
        public int ScorePerCellInLineClear => _scorePerCellInLineClear;
        public float CascadeMultiplierStep => _cascadeMultiplierStep;
        public int TargetScore => _targetScore;

        public float JokerSpawnChance => Mathf.Clamp01(_jokerSpawnChance);
        public float BombJokerSpawnChance => Mathf.Clamp01(_bombJokerSpawnChance);
        public float SkullSpawnChance => Mathf.Clamp01(_skullSpawnChance);
        public float SkullTimePenaltySeconds => Mathf.Max(0f, _skullTimePenaltySeconds);
        public TutorialStepDefinition[] TutorialSteps => _tutorialSteps ?? Array.Empty<TutorialStepDefinition>();

        private void OnValidate()
        {
            _jokerSpawnChance = Mathf.Clamp01(_jokerSpawnChance);
            _bombJokerSpawnChance = Mathf.Clamp01(_bombJokerSpawnChance);
            _skullSpawnChance = Mathf.Clamp01(_skullSpawnChance);
        }

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
