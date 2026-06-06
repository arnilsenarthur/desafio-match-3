using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.UI.Views;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Controllers
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField]
        private MainMenuView _mainMenuPanel;

        [SerializeField]
        private UiPanelView _difficultyPanel;

        [SerializeField]
        private SettingsPanelView _settingsPanel;

        private void Start() => ShowMainMenu();

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
