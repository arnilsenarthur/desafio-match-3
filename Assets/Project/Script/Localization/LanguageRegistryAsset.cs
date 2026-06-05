using System;
using System.Collections.Generic;
using Gazeus.DesafioMatch3.Misc;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Localization
{
    public class LanguageRegistryAsset : ScriptableObject
    {
        [SerializeField]
        private SerializableDictionary<string, string> _entries = new();

        public void SetEntries(KeyValuePair<string, string>[] entries)
        {
            _entries.Clear();

            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                KeyValuePair<string, string> entry = entries[i];
                if (string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                _entries.Set(entry.Key, entry.Value);
            }
        }

        public bool TryGetDisplayName(string languageCode, out string displayName) =>
            _entries.TryGetValue(languageCode, out displayName);
    }
}
