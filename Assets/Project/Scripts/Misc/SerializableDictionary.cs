using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Misc
{
    [Serializable]
    public abstract class SerializableDictionary
    {
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : SerializableDictionary, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> _keys = new();

        [SerializeField]
        private List<TValue> _values = new();

        private Dictionary<TKey, TValue> _lookup;

        public int Count => EnsureLookup().Count;

        public IEnumerable<TKey> Keys => EnsureLookup().Keys;

        public IEnumerable<TValue> Values => EnsureLookup().Values;

        public void Clear()
        {
            _keys.Clear();
            _values.Clear();
            _lookup?.Clear();
        }

        public bool ContainsKey(TKey key) => EnsureLookup().ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => EnsureLookup().TryGetValue(key, out value);

        public void Set(TKey key, TValue value) => EnsureLookup()[key] = value;

        public bool Remove(TKey key)
        {
            bool removed = EnsureLookup().Remove(key);
            if (removed)
            {
                SyncListsFromLookup();
            }

            return removed;
        }

        public void OnBeforeSerialize() => SyncListsFromLookup();

        public void OnAfterDeserialize() => RebuildLookupFromLists();

        private Dictionary<TKey, TValue> EnsureLookup()
        {
            if (_lookup == null)
            {
                RebuildLookupFromLists();
            }

            return _lookup;
        }

        private void RebuildLookupFromLists()
        {
            int count = Mathf.Min(_keys.Count, _values.Count);
            _lookup = new Dictionary<TKey, TValue>(count);

            for (int i = 0; i < count; i++)
            {
                _lookup[_keys[i]] = _values[i];
            }
        }

        private void SyncListsFromLookup()
        {
            if (_lookup == null)
            {
                return;
            }

            _keys.Clear();
            _values.Clear();

            foreach (KeyValuePair<TKey, TValue> pair in _lookup)
            {
                _keys.Add(pair.Key);
                _values.Add(pair.Value);
            }
        }
    }
}
