using System;

namespace Gazeus.DesafioMatch3.App
{
    public static class SceneLoader
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameplayScene = "Gameplay";

        public static void LoadMainMenu() =>
            SceneTransitionService.LoadScene(MainMenuScene, GameRunContext.Clear);

        public static void LoadGameplay() =>
            SceneTransitionService.LoadScene(GameplayScene);
    }
}
