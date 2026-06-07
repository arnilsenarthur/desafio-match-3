using System.Collections;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Audio;
using Gazeus.DesafioMatch3.UI.Views;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Controllers
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField]
        private MainMenuPanelView _mainMenuPanel;

        [SerializeField]
        private DifficultyPanelView _difficultyPanel;

        [SerializeField]
        private SettingsPanelView _settingsPanel;

        private void Awake()
        {
            if (_difficultyPanel == null)
            {
                _difficultyPanel = FindFirstObjectByType<DifficultyPanelView>(FindObjectsInactive.Include);
            }
        }

        private void Start() => StartCoroutine(StartRoutine());

        private IEnumerator StartRoutine()
        {
            AudioService.PlayMusic(AudioKeys.MusicMainMenu);

            yield return null;
            ShowMainMenu();

            SceneTransitionService.SetLoadingVisible(false);
            Coroutine reveal = StartCoroutine(SceneTransitionService.Reveal());

            if (_mainMenuPanel != null)
            {
                yield return _mainMenuPanel.WaitForEnterAnimation();
            }

            yield return reveal;
        }

        public void ShowMainMenu()
        {
            _difficultyPanel?.Hide();
            _mainMenuPanel?.Show();
        }

        public void ShowDifficultySelection()
        {
            _mainMenuPanel?.Hide();
            _difficultyPanel?.Show();
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
    }
}
