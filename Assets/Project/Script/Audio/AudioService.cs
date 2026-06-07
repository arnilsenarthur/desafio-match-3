using System;
using System.Collections.Generic;
using Gazeus.DesafioMatch3.App;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Audio
{
    public static class AudioService
    {
        public const int StartingAudioSources = 5;

        private const string RegistryResourcePath = "Audio/AudioRegistry";
        private const float DefaultMusicCrossfadeDuration = 1f;

        private static AudioRegistryAsset _registry;
        private static AudioServiceHost _host;
        private static bool _initialized;
        private static bool _settingsHooked;
        private static readonly Dictionary<string, float> _lastSfxPlayTimes = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            if (_settingsHooked)
            {
                SettingsService.Changed -= OnSettingsChanged;
            }

            _host = null;
            _registry = null;
            _initialized = false;
            _settingsHooked = false;
            _lastSfxPlayTimes.Clear();
        }

        public static int ActiveSfxSourceCount => _host != null ? _host.ActiveSfxSourceCount : 0;

        public static int TotalSfxSourceCount => _host != null ? _host.TotalSfxSourceCount : 0;

        public static bool Play(string key, float volumeScale = 1f)
        {
            if (!TryGetEntry(key, out AudioRegistryEntry entry))
            {
                return false;
            }

            return entry.IsMusic
                ? PlayMusic(key, DefaultMusicCrossfadeDuration, volumeScale)
                : PlaySfx(key, volumeScale);
        }

        public static bool PlaySfx(string key, float volumeScale = 1f)
        {
            EnsureInitialized();
            EnsureHost();

            if (!TryGetEntry(key, out AudioRegistryEntry entry))
            {
                Debug.LogWarning($"Missing audio key '{key}'.");
                return false;
            }

            if (entry.IsMusic)
            {
                Debug.LogWarning($"Audio key '{key}' is music. Use PlayMusic instead.");
                return false;
            }

            if (entry.Clip == null)
            {
                Debug.LogWarning($"Audio key '{key}' has no clip assigned.");
                return false;
            }

            SettingsService.EnsureLoaded();

            if (_host == null)
            {
                return false;
            }

            return _host.PlaySfx(entry, key, SettingsService.VfxVolume * volumeScale);
        }

        public static bool PlaySfxRateLimited(string key, float minIntervalSeconds, float volumeScale = 1f)
        {
            if (minIntervalSeconds <= 0f)
            {
                return PlaySfx(key, volumeScale);
            }

            float now = Time.unscaledTime;
            if (_lastSfxPlayTimes.TryGetValue(key, out float lastPlayTime) &&
                now - lastPlayTime < minIntervalSeconds)
            {
                return false;
            }

            _lastSfxPlayTimes[key] = now;
            return PlaySfx(key, volumeScale);
        }

        public static bool PlayMusic(
            string key,
            float crossfadeDuration = DefaultMusicCrossfadeDuration,
            float volumeScale = 1f)
        {
            EnsureInitialized();
            EnsureHost();

            if (!TryGetEntry(key, out AudioRegistryEntry entry))
            {
                Debug.LogWarning($"Missing audio key '{key}'.");
                return false;
            }

            if (!entry.IsMusic)
            {
                Debug.LogWarning($"Audio key '{key}' is not marked as music.");
                return false;
            }

            if (entry.Clip == null)
            {
                return false;
            }

            SettingsService.EnsureLoaded();

            if (_host == null)
            {
                return false;
            }

            return _host.PlayMusic(entry, key, crossfadeDuration, SettingsService.MusicVolume * volumeScale);
        }

        public static void StopMusic(float fadeOutDuration = DefaultMusicCrossfadeDuration)
        {
            if (_host == null)
            {
                return;
            }

            SettingsService.EnsureLoaded();
            _host.StopMusic(fadeOutDuration, SettingsService.MusicVolume);
        }

        internal static bool TryGetEntry(string key, out AudioRegistryEntry entry)
        {
            entry = null;

            if (string.IsNullOrEmpty(key) || _registry == null)
            {
                return false;
            }

            return _registry.TryGet(key, out entry);
        }

        internal static void RegisterHost(AudioServiceHost host)
        {
            if (host == null)
            {
                return;
            }

            _host = host;
        }

        internal static void UnregisterHost(AudioServiceHost host)
        {
            if (_host == host)
            {
                _host = null;
            }
        }

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            SettingsService.EnsureLoaded();
            _registry = Resources.Load<AudioRegistryAsset>(RegistryResourcePath);

            if (_registry == null)
            {
                Debug.LogError($"Missing Resources/{RegistryResourcePath}.asset audio registry.");
            }

            EnsureHost();
            HookSettingsChanges();
            _initialized = true;
        }

        private static void EnsureHost()
        {
            if (_host != null)
            {
                return;
            }

            _host = UnityEngine.Object.FindFirstObjectByType<AudioServiceHost>(FindObjectsInactive.Include);

            if (_host == null)
            {
                Debug.LogError(
                    "AudioServiceHost not found. Add the AudioServiceHost prefab to the scene.");
            }
        }

        private static void HookSettingsChanges()
        {
            if (_settingsHooked)
            {
                return;
            }

            SettingsService.Changed += OnSettingsChanged;
            _settingsHooked = true;
        }

        private static void OnSettingsChanged(SettingChangedEventArgs args)
        {
            if (_host == null)
            {
                return;
            }

            switch (args.Id)
            {
                case SettingId.VfxVolume:
                    _host.ApplySfxVolume(SettingsService.VfxVolume);
                    break;
                case SettingId.MusicVolume:
                    _host.ApplyMusicVolume(SettingsService.MusicVolume);
                    break;
            }
        }
    }
}
