using Gazeus.DesafioMatch3.Misc;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Audio
{
    [CreateAssetMenu(fileName = "AudioRegistry", menuName = "Audio/Audio Registry")]
    public class AudioRegistryAsset : ScriptableObject
    {
        [SerializeField]
        private SerializableDictionary<string, AudioRegistryEntry> _entries = new();

        public bool TryGet(string key, out AudioRegistryEntry entry) => _entries.TryGetValue(key, out entry);

        public void ClearEntries() => _entries.Clear();

        public void SetEntry(string key, AudioClip clip, bool isMusic, float volume = 1f)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var entry = new AudioRegistryEntry();
            entry.Configure(clip, isMusic, volume);
            _entries.Set(key, entry);
        }
    }
}
