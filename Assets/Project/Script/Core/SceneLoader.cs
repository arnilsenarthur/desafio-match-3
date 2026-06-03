using UnityEngine.SceneManagement;

namespace Gazeus.DesafioMatch3.Core
{
    public static class SceneLoader
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameplayScene = "Gameplay";

        public static void LoadMainMenu()
        {
            GameRunContext.Clear();
            SceneManager.LoadScene(MainMenuScene);
        }

        public static void LoadGameplay()
        {
            SceneManager.LoadScene(GameplayScene);
        }

        public static void ReloadGameplay()
        {
            SceneManager.LoadScene(GameplayScene);
        }
    }
}
