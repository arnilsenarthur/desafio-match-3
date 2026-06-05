using System;
using Gazeus.DesafioMatch3.Localization;
using UnityEngine;

namespace Gazeus.DesafioMatch3.App
{
    public static class SettingsService
    {
        private const string KeyLanguage = "settings.language";
        private const string KeyVfxVolume = "settings.vfx_volume";
        private const string KeyMusicVolume = "settings.music_volume";
        private const string KeyAnimationSpeed = "settings.animation_speed";
        private const string KeyPlayTutorialNextTime = "settings.play_tutorial_next_time";

        private const int AnimationSpeedMin = 0;
        private const int AnimationSpeedMax = 4;
        private const int DefaultAnimationSpeed = 2;

        private static readonly float[] AnimationSpeedMultipliers = { 0.25f, 0.5f, 1f, 2f, 4f };

        private static bool _loaded;

        public static event Action<SettingChangedEventArgs> Changed;

        public static string Language { get; private set; } = LanguageCodes.Default;
        public static float VfxVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;
        public static int AnimationSpeed { get; private set; } = DefaultAnimationSpeed;
        public static bool PlayTutorialNextTime { get; private set; } = true;

        public static float AnimationSpeedMultiplier => AnimationSpeedMultipliers[AnimationSpeed];

        public static float AnimationDurationFactor => 1f / AnimationSpeedMultiplier;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            _loaded = false;
            Changed = null;
            Language = LanguageCodes.Default;
            VfxVolume = 1f;
            MusicVolume = 1f;
            AnimationSpeed = DefaultAnimationSpeed;
            PlayTutorialNextTime = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapBeforeSceneLoad()
        {
            LocalizationService.Initialize();
            EnsureLoaded();
            LocalizationService.SetLanguage(Language);
        }

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            Language = PlayerPrefs.GetString(KeyLanguage, LanguageCodes.Default);
            VfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVfxVolume, 1f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusicVolume, 1f));
            AnimationSpeed = Mathf.Clamp(
                PlayerPrefs.GetInt(KeyAnimationSpeed, DefaultAnimationSpeed),
                AnimationSpeedMin,
                AnimationSpeedMax);
            PlayTutorialNextTime = PlayerPrefs.GetInt(KeyPlayTutorialNextTime, 1) != 0;
            _loaded = true;
        }

        public static float ScaleDuration(float baseDuration) => baseDuration / AnimationSpeedMultiplier;

        public static void ConsumePlayTutorialNextTime() => SetPlayTutorialNextTime(false);

        public static void SetLanguage(string languageCode)
        {
            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(languageCode) || Language == languageCode)
            {
                return;
            }

            Language = languageCode;
            PlayerPrefs.SetString(KeyLanguage, languageCode);
            PlayerPrefs.Save();
            LocalizationService.SetLanguage(languageCode);
            RaiseChanged(SettingId.Language);
        }

        public static void SetVfxVolume(float volume)
        {
            EnsureLoaded();
            float clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(VfxVolume, clamped))
            {
                return;
            }

            VfxVolume = clamped;
            PlayerPrefs.SetFloat(KeyVfxVolume, clamped);
            PlayerPrefs.Save();
            RaiseChanged(SettingId.VfxVolume);
        }

        public static void SetMusicVolume(float volume)
        {
            EnsureLoaded();
            float clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(MusicVolume, clamped))
            {
                return;
            }

            MusicVolume = clamped;
            PlayerPrefs.SetFloat(KeyMusicVolume, clamped);
            PlayerPrefs.Save();
            RaiseChanged(SettingId.MusicVolume);
        }

        public static void SetAnimationSpeed(int level)
        {
            EnsureLoaded();
            int clamped = Mathf.Clamp(level, AnimationSpeedMin, AnimationSpeedMax);
            if (AnimationSpeed == clamped)
            {
                return;
            }

            AnimationSpeed = clamped;
            PlayerPrefs.SetInt(KeyAnimationSpeed, clamped);
            PlayerPrefs.Save();
            RaiseChanged(SettingId.AnimationSpeed);
        }

        public static void SetPlayTutorialNextTime(bool value)
        {
            EnsureLoaded();
            if (PlayTutorialNextTime == value)
            {
                return;
            }

            PlayTutorialNextTime = value;
            PlayerPrefs.SetInt(KeyPlayTutorialNextTime, value ? 1 : 0);
            PlayerPrefs.Save();
            RaiseChanged(SettingId.PlayTutorialNextTime);
        }

        private static void RaiseChanged(SettingId id) => Changed?.Invoke(new SettingChangedEventArgs(id));
    }
}
