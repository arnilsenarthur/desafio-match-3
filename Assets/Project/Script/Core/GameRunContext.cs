namespace Gazeus.DesafioMatch3.Core
{
    public static class GameRunContext
    {
        public static string SelectedDifficultyId { get; private set; }

        public static bool HasSelectedDifficulty => !string.IsNullOrEmpty(SelectedDifficultyId);

        public static void SelectDifficulty(string difficultyId)
        {
            SelectedDifficultyId = difficultyId;
        }

        public static void Clear()
        {
            SelectedDifficultyId = null;
        }
    }
}
