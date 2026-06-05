using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Localization
{
    public class LanguageRegistryAsset : ScriptableObject
    {
        [SerializeField]
        private LangEntry[] _entries = Array.Empty<LangEntry>();

        private Dictionary<string, string> _lookup;

        public void SetEntries(LangEntry[] entries)
        {
            _entries = entries ?? Array.Empty<LangEntry>();
            _lookup = null;
        }

        public bool TryGetDisplayName(string languageCode, out string displayName)
        {
            EnsureLookup();
            return _lookup.TryGetValue(languageCode, out displayName);
        }

        private void EnsureLookup()
        {
            if (_lookup == null)
            {
                _lookup = LangEntryLookup.Build(_entries);
            }
        }
    }
}
