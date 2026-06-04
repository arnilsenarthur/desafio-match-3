namespace Gazeus.DesafioMatch3.UI
{
    public static class UiText
    {
        public const string CountdownGo = "GO!";
        public const string StatusBoardReshuffled = "Board reshuffled!";

        public const string ConfirmRestartTitle = "Restart Game?";
        public const string ConfirmRestartMessage = "Current progress will be lost.";
        public const string ConfirmRestartButton = "Restart";

        public const string ConfirmMainMenuTitle = "Leave Game?";
        public const string ConfirmMainMenuMessage = "Return to main menu? Current progress will be lost.";
        public const string ConfirmMainMenuButton = "Main Menu";

        public const string GameOverNewHighScore = "\nNew high score!";
        public const string GameOverGoalReached = "Goal reached!";
        public const string GameOverTimeUp = "Time's up!";
        public const string GameOverDefault = "Game over";

        public static string ScoreLine(int score, int best) => $"Score: {score}  Best: {best}";

        public static string TimeLine(int minutes, int seconds) => $"Time: {minutes:00}:{seconds:00}";

        public static string GameOverMessage(string reasonLine, int score, int best, bool isNewHighScore) =>
            $"{reasonLine}\nScore: {score}\nBest: {best}{(isNewHighScore ? GameOverNewHighScore : string.Empty)}";
    }
}
