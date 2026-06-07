using System;
using System.Collections;
using Gazeus.DesafioMatch3.Data;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public static class GameplayCountdownRunner
    {
        public static IEnumerator Run(
            GameConfig config,
            Action<bool> setCountdownActive,
            UnityEngine.Object owner = null)
        {
            if (config == null || !GameService.IsActive)
            {
                yield break;
            }

            setCountdownActive(true);

            if (config.DelayBeforeCountdown > 0f)
            {
                yield return WaitSeconds(config.DelayBeforeCountdown, owner);
                if (!IsOwnerAlive(owner))
                {
                    yield break;
                }
            }

            for (int step = config.CountdownLength; step >= 1; step--)
            {
                if (!IsOwnerAlive(owner))
                {
                    yield break;
                }

                GameService.NotifyCountdownChanged(CountdownChangedEventArgs.ShowNumber(step));
                yield return WaitSeconds(config.CountdownStepDuration, owner);
            }

            if (!IsOwnerAlive(owner))
            {
                yield break;
            }

            if (config.GoDisplayDuration > 0f)
            {
                GameService.NotifyCountdownChanged(CountdownChangedEventArgs.ShowGo());
                yield return WaitSeconds(config.GoDisplayDuration, owner);
            }

            if (IsOwnerAlive(owner))
            {
                GameService.NotifyCountdownChanged(CountdownChangedEventArgs.Hidden);
            }

            setCountdownActive(false);
        }

        private static IEnumerator WaitSeconds(float duration, UnityEngine.Object owner)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!IsOwnerAlive(owner))
                {
                    yield break;
                }

                if (!GameService.IsPaused)
                {
                    elapsed += Time.deltaTime;
                }

                yield return null;
            }
        }

        private static bool IsOwnerAlive(UnityEngine.Object owner)
        {
            if (owner == null)
            {
                return true;
            }

            return owner != null;
        }
    }
}
