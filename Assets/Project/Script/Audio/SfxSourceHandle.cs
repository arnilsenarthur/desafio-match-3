using Gazeus.DesafioMatch3.Misc;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxSourceHandle : MonoBehaviour, IPoolKey<int>
    {
        [SerializeField]
        private AudioSource _source;

        private string _activeKey;
        private bool _inUse;

        public int PoolKey { get; set; }

        public bool InUse => _inUse;

        public string ActiveKey => _activeKey;

        public AudioSource Source
        {
            get
            {
                if (_source == null)
                {
                    _source = GetComponent<AudioSource>();
                }

                return _source;
            }
        }

        public void BeginPlayback(string key, AudioClip clip, float volume)
        {
            _activeKey = key;
            _inUse = true;

            AudioSource source = Source;
            source.clip = clip;
            source.loop = false;
            source.volume = volume;
            source.Play();
        }

        public void StopPlayback()
        {
            AudioSource source = Source;
            source.Stop();
            source.clip = null;
            _activeKey = null;
            _inUse = false;
        }

        public void ResetForPool()
        {
            StopPlayback();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _source = GetComponent<AudioSource>();
            if (_source != null)
            {
                _source.playOnAwake = false;
                _source.loop = false;
            }
        }
#endif
    }
}
