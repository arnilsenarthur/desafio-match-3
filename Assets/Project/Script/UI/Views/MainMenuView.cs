using System.Collections;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Localization;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class MainMenuView : UiPanelView
    {
        [SerializeField]
        private GameConfig _gameConfig;

        [SerializeField]
        private TMP_Text _highScoreDifficultyLabel;

        [SerializeField]
        private TMP_Text _highScoreValueLabel;

        [SerializeField]
        private float _highScoreCycleSeconds = 3f;

        private Coroutine _highScoreCycleCoroutine;
        private int _highScoreCycleIndex;

        private void OnEnable() => LocalizationService.LanguageChanged += OnLanguageChanged;

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            StopHighScoreCycle();
        }

        protected override void OnBeforeShow()
        {
            StartHighScoreCycle();
        }

        protected override void OnAfterHide()
        {
            StopHighScoreCycle();
        }

        private void OnLanguageChanged()
        {
            if (!this || !IsVisible)
            {
                return;
            }

            if (CanCycleHighScores())
            {
                ShowHighScoreEntry(_highScoreCycleIndex);
            }
        }

        private void StartHighScoreCycle()
        {
            StopHighScoreCycle();

            if (!CanCycleHighScores())
            {
                ClearHighScoreLabels();
                return;
            }

            _highScoreCycleIndex = 0;
            ShowHighScoreEntry(_highScoreCycleIndex);
            _highScoreCycleCoroutine = StartCoroutine(CycleHighScores());
        }

        private void StopHighScoreCycle()
        {
            if (_highScoreCycleCoroutine == null)
            {
                return;
            }

            StopCoroutine(_highScoreCycleCoroutine);
            _highScoreCycleCoroutine = null;
        }

        private IEnumerator CycleHighScores()
        {
            WaitForSeconds wait = new(_highScoreCycleSeconds);

            while (true)
            {
                yield return wait;

                if (!this || !isActiveAndEnabled || !IsVisible || !CanCycleHighScores())
                {
                    yield break;
                }

                _highScoreCycleIndex = (_highScoreCycleIndex + 1) % _gameConfig.Difficulties.Length;
                ShowHighScoreEntry(_highScoreCycleIndex);
            }
        }

        private bool CanCycleHighScores() =>
            _gameConfig != null &&
            _highScoreDifficultyLabel != null &&
            _highScoreValueLabel != null &&
            _gameConfig.Difficulties is { Length: > 0 };

        private void ShowHighScoreEntry(int index)
        {
            GameDifficultySettings difficulty = _gameConfig.Difficulties[index];
            _highScoreDifficultyLabel.text = LocalizationService.LocalizeDifficulty(difficulty.Id);
            _highScoreValueLabel.text = HighScoreStorage.Get(difficulty.Id).ToString();
        }

        private void ClearHighScoreLabels()
        {
            if (_highScoreDifficultyLabel != null)
            {
                _highScoreDifficultyLabel.text = string.Empty;
            }

            if (_highScoreValueLabel != null)
            {
                _highScoreValueLabel.text = string.Empty;
            }
        }
    }
}
