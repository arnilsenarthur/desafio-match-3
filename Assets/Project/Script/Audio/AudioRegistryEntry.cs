using System;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Audio
{
    [Serializable]
    public class AudioRegistryEntry
    {
        [SerializeField]
        private AudioClip _clip;

        [SerializeField]
        private bool _isMusic;

        [SerializeField]
        [Range(0f, 1f)]
        private float _volume = 1f;

        public AudioClip Clip => _clip;

        public bool IsMusic => _isMusic;

        public float Volume => _volume;

        public void Configure(AudioClip clip, bool isMusic, float volume = 1f)
        {
            _clip = clip;
            _isMusic = isMusic;
            _volume = Mathf.Clamp01(volume);
        }
    }
}
