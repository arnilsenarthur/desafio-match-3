using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Audio
{
    public sealed class AudioServiceHost : MonoBehaviour
    {
        private const float MinCrossfadeDuration = 0.01f;

        private static AudioServiceHost _instance;

        [SerializeField]
        private SfxSourcePool _sfxPool;

        [SerializeField]
        private GameObject _sfxSourcePrefab;

        [SerializeField]
        private int _startingSfxSources = 5;

        [SerializeField]
        private AudioSource _musicSourceA;

        [SerializeField]
        private AudioSource _musicSourceB;

        private readonly Dictionary<SfxSourceHandle, Coroutine> _releaseRoutines = new();

        private bool _musicAIsActive = true;
        private string _currentMusicKey;
        private Coroutine _musicCrossfadeRoutine;

        public int ActiveSfxSourceCount => _sfxPool != null ? _sfxPool.ActiveCount : 0;

        public int TotalSfxSourceCount => _sfxPool != null ? _sfxPool.TotalCount : 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Configure(_startingSfxSources);
            AudioService.RegisterHost(this);
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            StopAllReleaseRoutines();
            _instance = null;
            AudioService.UnregisterHost(this);
        }

        public void Configure(int startingSfxSources)
        {
            EnsureSfxPool();
            EnsureMusicSources();
            _sfxPool.Configure(_sfxSourcePrefab, Mathf.Max(1, startingSfxSources));
        }

        public bool PlaySfx(AudioRegistryEntry entry, string key, float volumeScale)
        {
            if (entry == null || entry.Clip == null || entry.IsMusic || _sfxPool == null)
            {
                return false;
            }

            SfxSourceHandle handle = _sfxPool.Rent();
            handle.BeginPlayback(key, entry.Clip, entry.Volume * volumeScale);

            if (_releaseRoutines.TryGetValue(handle, out Coroutine existingRoutine) && existingRoutine != null)
            {
                StopCoroutine(existingRoutine);
            }

            Coroutine releaseRoutine = StartCoroutine(ReleaseSfxAfter(handle, entry.Clip.length));
            _releaseRoutines[handle] = releaseRoutine;
            return true;
        }

        public bool PlayMusic(AudioRegistryEntry entry, string key, float crossfadeDuration, float volumeScale)
        {
            if (entry == null || entry.Clip == null || !entry.IsMusic)
            {
                return false;
            }

            EnsureMusicSources();

            if (_currentMusicKey == key &&
                (_musicSourceA.isPlaying || _musicSourceB.isPlaying))
            {
                ApplyMusicVolume(volumeScale);
                return true;
            }

            AudioSource incoming = _musicAIsActive ? _musicSourceB : _musicSourceA;
            AudioSource outgoing = _musicAIsActive ? _musicSourceA : _musicSourceB;
            _musicAIsActive = !_musicAIsActive;
            _currentMusicKey = key;

            incoming.clip = entry.Clip;
            incoming.loop = true;
            incoming.volume = 0f;
            incoming.Play();

            if (_musicCrossfadeRoutine != null)
            {
                StopCoroutine(_musicCrossfadeRoutine);
            }

            float targetVolume = entry.Volume * volumeScale;
            _musicCrossfadeRoutine = StartCoroutine(CrossfadeMusic(outgoing, incoming, targetVolume, crossfadeDuration));
            return true;
        }

        public void StopMusic(float fadeOutDuration, float volumeScale)
        {
            if (_musicCrossfadeRoutine != null)
            {
                StopCoroutine(_musicCrossfadeRoutine);
                _musicCrossfadeRoutine = null;
            }

            _currentMusicKey = null;
            EnsureMusicSources();

            if (fadeOutDuration <= MinCrossfadeDuration)
            {
                _musicSourceA.Stop();
                _musicSourceB.Stop();
                _musicSourceA.clip = null;
                _musicSourceB.clip = null;
                return;
            }

            _musicCrossfadeRoutine = StartCoroutine(FadeOutMusic(fadeOutDuration, volumeScale));
        }

        public void ApplySfxVolume(float volumeScale)
        {
            if (_sfxPool == null)
            {
                return;
            }

            _sfxPool.ForEachActive(handle =>
            {
                if (!AudioService.TryGetEntry(handle.ActiveKey, out AudioRegistryEntry entry))
                {
                    return;
                }

                handle.Source.volume = entry.Volume * volumeScale;
            });
        }

        public void ApplyMusicVolume(float volumeScale)
        {
            EnsureMusicSources();

            AudioSource active = _musicAIsActive ? _musicSourceA : _musicSourceB;
            if (active == null || !active.isPlaying || string.IsNullOrEmpty(_currentMusicKey))
            {
                return;
            }

            if (!AudioService.TryGetEntry(_currentMusicKey, out AudioRegistryEntry entry))
            {
                return;
            }

            active.volume = entry.Volume * volumeScale;
        }

        private void EnsureSfxPool()
        {
            if (_sfxPool != null)
            {
                return;
            }

            _sfxPool = GetComponent<SfxSourcePool>();
            if (_sfxPool == null)
            {
                _sfxPool = gameObject.AddComponent<SfxSourcePool>();
            }
        }

        private void EnsureMusicSources()
        {
            if (_musicSourceA == null)
            {
                _musicSourceA = CreateMusicSource("MusicA");
            }

            if (_musicSourceB == null)
            {
                _musicSourceB = CreateMusicSource("MusicB");
            }
        }

        private AudioSource CreateMusicSource(string sourceName)
        {
            Transform existing = transform.Find(sourceName);
            if (existing != null && existing.TryGetComponent(out AudioSource existingSource))
            {
                ConfigureMusicSource(existingSource);
                return existingSource;
            }

            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            ConfigureMusicSource(source);
            return source;
        }

        private static void ConfigureMusicSource(AudioSource source)
        {
            source.loop = true;
            source.playOnAwake = false;
        }

        private IEnumerator ReleaseSfxAfter(SfxSourceHandle handle, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (handle != null)
            {
                _sfxPool?.Return(handle);
            }

            _releaseRoutines.Remove(handle);
        }

        private void StopAllReleaseRoutines()
        {
            foreach (KeyValuePair<SfxSourceHandle, Coroutine> pair in _releaseRoutines)
            {
                if (pair.Value != null)
                {
                    StopCoroutine(pair.Value);
                }
            }

            _releaseRoutines.Clear();
        }

        private IEnumerator CrossfadeMusic(
            AudioSource outgoing,
            AudioSource incoming,
            float targetVolume,
            float duration)
        {
            duration = Mathf.Max(MinCrossfadeDuration, duration);
            float startIncoming = incoming.volume;
            float startOutgoing = outgoing != null && outgoing.isPlaying ? outgoing.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                incoming.volume = Mathf.Lerp(startIncoming, targetVolume, t);

                if (outgoing != null && outgoing.isPlaying)
                {
                    outgoing.volume = Mathf.Lerp(startOutgoing, 0f, t);
                }

                yield return null;
            }

            incoming.volume = targetVolume;

            if (outgoing != null)
            {
                outgoing.Stop();
                outgoing.clip = null;
                outgoing.volume = 0f;
            }

            _musicCrossfadeRoutine = null;
        }

        private IEnumerator FadeOutMusic(float duration, float volumeScale)
        {
            duration = Mathf.Max(MinCrossfadeDuration, duration);
            float startA = _musicSourceA.isPlaying ? _musicSourceA.volume : 0f;
            float startB = _musicSourceB.isPlaying ? _musicSourceB.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - elapsed / duration;

                if (_musicSourceA.isPlaying)
                {
                    _musicSourceA.volume = startA * t;
                }

                if (_musicSourceB.isPlaying)
                {
                    _musicSourceB.volume = startB * t;
                }

                yield return null;
            }

            _musicSourceA.Stop();
            _musicSourceB.Stop();
            _musicSourceA.clip = null;
            _musicSourceB.clip = null;
            _musicSourceA.volume = 0f;
            _musicSourceB.volume = 0f;
            _musicCrossfadeRoutine = null;
        }
    }
}
