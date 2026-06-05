using System.Collections;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Localization;
using Gazeus.DesafioMatch3.UI.Views;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Controllers
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField]
        private GameConfig _gameConfig;

        [SerializeField]
        private UiPanelView _mainMenuPanel;

        [SerializeField]
        private UiPanelView _difficultyPanel;

        [SerializeField]
        private SettingsPanelController _settingsPanel;

        [SerializeField]
        private TMP_Text _highScoreDifficultyLabel;

        [SerializeField]
        private TMP_Text _highScoreValueLabel;

        [SerializeField]
        private float _highScoreCycleSeconds = 3f;

        private Coroutine _highScoreCycleCoroutine;
        private int _highScoreCycleIndex;

        private void Start() => ShowMainMenu();

        private void OnEnable() => LocalizationService.LanguageChanged += OnLanguageChanged;

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            StopHighScoreCycle();
        }

        public void ShowMainMenu()
        {
            _difficultyPanel.Hide();
            _mainMenuPanel.Show();
            StartHighScoreCycle();
        }

        public void ShowDifficultySelection()
        {
            StopHighScoreCycle();
            _mainMenuPanel.Hide();
            _difficultyPanel.Show();
        }

        public void StartGame(string difficultyId)
        {
            GameRunContext.SelectDifficulty(difficultyId);
            SceneLoader.LoadGameplay();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowSettings() => _settingsPanel?.Show();

        private void OnLanguageChanged()
        {
            if (!this)
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

                if (!this || !isActiveAndEnabled || !CanCycleHighScores())
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
