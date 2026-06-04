using System;
using System.Collections;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.UI;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public static class GameplayCountdownRunner
    {
        public static IEnumerator Run(
            GameConfig config,
            GameEvents events,
            Func<bool> isPaused,
            Action<bool> setCountdownActive)
        {
            setCountdownActive(true);

            if (config.DelayBeforeCountdown > 0f)
            {
                yield return WaitSeconds(config.DelayBeforeCountdown, isPaused);
            }

            for (int step = config.CountdownLength; step >= 1; step--)
            {
                events.RaiseCountdownChanged(CountdownChangedEventArgs.Show(step.ToString()));
                yield return WaitSeconds(config.CountdownStepDuration, isPaused);
            }

            if (config.GoDisplayDuration > 0f)
            {
                events.RaiseCountdownChanged(CountdownChangedEventArgs.Show(UiText.CountdownGo));
                yield return WaitSeconds(config.GoDisplayDuration, isPaused);
            }

            events.RaiseCountdownChanged(CountdownChangedEventArgs.Hidden);
            setCountdownActive(false);
        }

        private static IEnumerator WaitSeconds(float duration, Func<bool> isPaused)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
