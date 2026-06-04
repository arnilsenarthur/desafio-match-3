using UnityEngine.SceneManagement;

namespace Gazeus.DesafioMatch3.App
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

        public static void LoadGameplay() => SceneManager.LoadScene(GameplayScene);
    }
}
