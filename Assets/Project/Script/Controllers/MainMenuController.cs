using Gazeus.DesafioMatch3.Core;
using Gazeus.DesafioMatch3.Views;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UiPanelView _mainMenuPanel;
        [SerializeField] private UiPanelView _difficultyPanel;

        private void Start()
        {
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            _difficultyPanel.Hide();
            _mainMenuPanel.Show();
        }

        public void ShowDifficultySelection()
        {
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
    }
}
