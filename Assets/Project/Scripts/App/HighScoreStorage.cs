using UnityEngine;

namespace Gazeus.DesafioMatch3.App
{
    public static class HighScoreStorage
    {
        private const string KeyPrefix = "desafio_match3.highscore.";

        public static int Get(string difficultyId)
        {
            if (string.IsNullOrEmpty(difficultyId))
            {
                return 0;
            }

            return PlayerPrefs.GetInt(KeyPrefix + difficultyId, 0);
        }

        public static int GetDisplayBest(string difficultyId, int currentScore) =>
            Mathf.Max(Get(difficultyId), currentScore);

        public static bool TrySetHighScore(string difficultyId, int score)
        {
            if (string.IsNullOrEmpty(difficultyId))
            {
                return false;
            }

            int previous = Get(difficultyId);
            if (score <= previous)
            {
                return false;
            }

            PlayerPrefs.SetInt(KeyPrefix + difficultyId, score);
            PlayerPrefs.Save();
            return true;
        }
    }
}
