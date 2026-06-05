using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Localization
{
    public class LangTableAsset : ScriptableObject
    {
        [SerializeField]
        private LangEntry[] _entries = Array.Empty<LangEntry>();

        private Dictionary<string, string> _lookup;

        public void SetEntries(LangEntry[] entries)
        {
            _entries = entries ?? Array.Empty<LangEntry>();
            _lookup = null;
        }

        public bool TryGet(string key, out string value)
        {
            EnsureLookup();
            return _lookup.TryGetValue(key, out value);
        }

        public IEnumerable<string> GetKeys()
        {
            EnsureLookup();
            return _lookup.Keys;
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
