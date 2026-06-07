using Gazeus.DesafioMatch3.Gameplay;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Audio
{
    public sealed class GameplayAudioController : MonoBehaviour
    {
        private bool _bound;

        private void OnEnable() => Bind();

        private void OnDisable() => Unbind();

        public void Bind()
        {
            Unbind();

            GameService.CascadeStep += OnCascadeStep;
            GameService.GameEnded += OnGameEnded;
            GameService.CountdownChanged += OnCountdownChanged;
            GameService.GameStarted += OnGameStarted;
            _bound = true;
        }

        public void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            GameService.CascadeStep -= OnCascadeStep;
            GameService.GameEnded -= OnGameEnded;
            GameService.CountdownChanged -= OnCountdownChanged;
            GameService.GameStarted -= OnGameStarted;
            _bound = false;
        }

        private void OnGameStarted(GameStartedEventArgs args) =>
            AudioService.PlayMusic(AudioKeys.MusicGameplay);

        private void OnGameEnded(GameEndedEventArgs args)
        {
            string key = args.IsNewHighScore
                ? AudioKeys.GameplayHighScore
                : AudioKeys.GameplayGameOver;

            AudioService.PlaySfx(key);
        }

        private void OnCountdownChanged(CountdownChangedEventArgs args)
        {
            if (!args.IsVisible)
            {
                return;
            }

            if (args.IsGo)
            {
                AudioService.PlaySfx(AudioKeys.GameplayGameStart);
                return;
            }

            AudioService.PlaySfx(AudioKeys.UiCheck);
        }

        private void OnCascadeStep(CascadeStepEventArgs args)
        {
            string key = IsSpecialMatch(args.Sequence)
                ? AudioKeys.GameplayMatchSpecial
                : AudioKeys.GameplayMatch;

            AudioService.PlaySfx(key);
        }

        private static bool IsSpecialMatch(BoardSequence sequence)
        {
            if (sequence == null)
            {
                return false;
            }

            if (sequence.ClearedRows.Count > 0 || sequence.ClearedColumns.Count > 0)
            {
                return true;
            }

            return sequence.MatchedPosition != null && sequence.MatchedPosition.Count >= 4;
        }
    }
}
