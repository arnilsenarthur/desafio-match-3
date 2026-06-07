using UnityEngine;

namespace Gazeus.DesafioMatch3.App
{
    public static class GameRunContext
    {
        public static string SelectedDifficultyId { get; private set; }

        public static bool HasSelectedDifficulty => !string.IsNullOrEmpty(SelectedDifficultyId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration() => Clear();

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
